using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Placement controller for the Building System (design-brief.md, 2026-08-08).
// Scoped to Foundation-only for this first pass — Wall/Pole/Door reuse the
// same BuildPiece/BuildSocket machinery later, not a second system.
//
// Two flows, matching the design doc exactly:
// - Free placement (nothing compatible nearby): LMB drops a ghost at the
//   camera raycast's hit point; scroll wheel rotates it in place; LMB again
//   confirms. Modeled as Following -> Locked -> confirm.
// - Edge-snapped placement (a compatible open BuildSocket in range): the
//   ghost snaps automatically, position and rotation both implied by the
//   socket, so a single LMB press confirms immediately.
//
// Deliberately NOT hidden like magic (see PlayerMagic/no-UI-hints) —
// Building is a learnable system, not a mystery mechanic, so this draws a
// real ghost and real messages.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerBuilding : MonoBehaviour
{
    private enum Phase { Following, Locked }

    [SerializeField] private BuildPiece[] allPieces;
    [SerializeField] private float interactRange = 10f;
    // Bumped 1.5 -> 2.5 (2026-08-20, found live: aiming at the middle of
    // an existing 5m Foundation's face -- the natural way to try tiling
    // one next to it -- put the crosshair ~2.5m from the actual edge
    // socket, well outside the old radius, so the snap silently never
    // fired and the piece fell through to free-placement instead
    // (visible seam/height mismatch). New value matches Foundation's own
    // half-width exactly, so aiming anywhere on the near half of an
    // adjacent piece now catches its edge socket.
    [SerializeField] private float snapRadius = 2.5f;
    // Foundation is a 5m x 5m square — half-width used to offset a newly
    // snapped panel directly outward from the socket it's attaching to.
    // Only correct for square, axis-aligned pieces like Foundation; Wall/
    // Door will need real per-socket alignment math when they're added.
    [SerializeField] private float panelHalfSize = 2.5f;
    [SerializeField] private float rotationStepDegrees = 90f;
    [SerializeField] private float scrollThreshold = 20f;
    [SerializeField] private float storageRange = 10f;
    private const float MessageDuration = 3f;

    // City Statue founding gate (VILLAGE_FLAG_PLANNING.md section 6) -- a
    // live precondition checked at placement time, not a lifetime counter.
    private const int CityFoundingRequiredHiredNpcs = 10;

    // City Statue's own Player Map reveal (PLAYER_MAP_PLANNING.md section
    // 1) -- flat, not tier-keyed like the Flag's own ladder, since the
    // Statue is a one-time milestone, not a craftable tier.
    private const float CityStatueRevealRadius = 125f;

    private PlayerInventory inventory;
    private PlayerSkills skills;
    private PlayerInteraction interaction;
    private PlayerBackpack backpackCarrier;
    // Both optional -- Fame/Map reveal are real hooks (City Statue Fame
    // grant, Flag/Statue placement revealing the Player Map), but building
    // itself shouldn't hard-require either just to place a Foundation.
    private PlayerFame fame;
    private PlayerMapExploration mapExploration;
    private readonly List<StorageBox> nearbyStorages = new List<StorageBox>();

    private BuildPiece armedPiece;
    private SocketType[] armedSocketTypes = System.Array.Empty<SocketType>();
    // Set only by ArmExistingPiece (2026-08-21, StorageBox pick-up-and-
    // move) -- when non-null, Confirm reuses this exact GameObject instead
    // of instantiating a fresh one from armedPiece.prefab, and skips the
    // ingredient cost/skill-XP grant that only make sense for actually
    // building something new. armedPiece is still set in this mode too,
    // purely for ghost sizing/groundOffset/socket-type data -- the same
    // BuildPiece asset the instance was originally built from.
    private GameObject existingInstanceToPlace;
    private GameObject ghost;
    private Phase phase = Phase.Following;
    private BuildSocket snappedSocket;
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;
    private bool currentValid;

    private Material ghostMaterial;
    private string message;
    private float messageExpireTime;

    public BuildPiece ArmedPiece => armedPiece;
    public IReadOnlyList<BuildPiece> AllPieces => allPieces;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
        interaction = GetComponent<PlayerInteraction>();
        backpackCarrier = GetComponent<PlayerBackpack>();
        fame = GetComponent<PlayerFame>();
        mapExploration = GetComponent<PlayerMapExploration>();
    }

    // Same reach as PlayerCrafting.ReachableInventories: main inventory
    // first, then an equipped Backpack, then any nearby StorageBox — a
    // player shouldn't have to empty their backpack onto the ground just
    // to place a Foundation.
    private IEnumerable<Inventory> ReachableInventories()
    {
        yield return inventory.Inventory;

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null)
            yield return backpack.Inventory;

        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);
        foreach (var box in nearbyStorages)
            yield return box.Inventory;
    }

    // Read by BuildScreen's tile grid for live "have N" counts — same
    // reach as ReachableInventories, not just the main inventory.
    public int GetAvailableCount(ItemDefinition item)
    {
        int total = 0;
        foreach (var inv in ReachableInventories())
            total += IngredientMatching.GetCount(inv, item);
        return total;
    }

    public bool CanPlace(BuildPiece piece) => LockReason(piece) == null;

    // Null if placeable, otherwise a short human reason -- read by
    // BuildScreen's warning label so a piece locked for a non-skill reason
    // (City Statue's founding conditions, or a future requiresCityStatus
    // piece) doesn't hit the old code's bare `piece.trainedSkill.
    // skillName` dereference on a null trainedSkill, and so the player
    // actually sees *why* it's locked instead of a generic message.
    public string LockReason(BuildPiece piece)
    {
        if (piece == null) return "unavailable";

        if (piece.trainedSkill != null)
        {
            int required = CraftTierScale.SkillRequirement(piece.unlockTier);
            if (skills.GetLevel(piece.trainedSkill) < required)
                return $"requires {piece.trainedSkill.skillName} {required}";
        }

        if (piece.requiresCityStatus && !CityStatue.Exists)
            return "requires an existing City Statue";

        if (piece.requiresCityFoundingConditions && !MeetsCityFoundingConditions())
            return $"requires a Masterwork Village Flag and {CityFoundingRequiredHiredNpcs} currently-hired NPCs";

        if (piece.requiresVillageFlagAndHiredNpc && !MeetsVillageFlagAndHiredNpcConditions())
            return "requires a Village Flag and at least 1 currently-hired NPC";

        return null;
    }

    // Live precondition, same "not cumulative" framing as
    // MeetsCityFoundingConditions -- a much lower bar (any Flag, 1 hire)
    // since this represents "you have a basic settlement going," not
    // "you founded a City."
    private bool MeetsVillageFlagAndHiredNpcConditions()
    {
        if (FindObjectsByType<VillageFlag>(FindObjectsSortMode.None).Length == 0) return false;

        foreach (var hiring in FindObjectsByType<NPCHiring>(FindObjectsSortMode.None))
            if (hiring.IsHired) return true;

        return false;
    }

    // Nearest VillageFlag to an arbitrary world position -- shared by the
    // one-per-Flag cap check below and (indirectly) by VillageVendor's own
    // CurrentMaxTier lookup pattern, just generalized to any position
    // rather than "nearest to this component."
    private VillageFlag FindNearestFlag(Vector3 position)
    {
        VillageFlag nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var flag in FindObjectsByType<VillageFlag>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(flag.transform.position, position);
            if (dist < nearestDist) { nearestDist = dist; nearest = flag; }
        }
        return nearest;
    }

    // One-per-Flag cap (2026-08-22, Ben's ask, both Vendor Stall and Bank
    // Box) -- true if any existing instance of T's own nearest Flag is the
    // same Flag the new one would be nearest to. Generic over T rather
    // than a bespoke check per structure type, since both share the exact
    // same "distinct economy per settlement" reasoning; Village Flags'
    // own existing minimum-spacing rule naturally caps how dense this can
    // get, same as Guild-vs-Guild territory clearance already does.
    private bool NearestFlagAlreadyHasStructure<T>(VillageFlag targetFlag) where T : Component
    {
        if (targetFlag == null) return false;
        foreach (var structure in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            // Exclude ghost previews -- ShowGhost does a full Instantiate of
            // armedPiece.prefab (only strips Colliders/BuildSockets, not
            // gameplay components), so a live VendorStall/BankBox sits on
            // the ghost the entire time the player is just aiming. Without
            // this, the ghost (always positioned right at the candidate
            // Flag) reads as "already exists" and blocks every placement
            // attempt, including the very first one -- found live 2026-08-22.
            // PlacedPiece is only ever added by Confirm() on a real
            // placement, never present on a ghost, so its absence is a
            // reliable "this isn't a real structure yet" signal.
            if (structure.GetComponent<PlacedPiece>() == null) continue;
            if (FindNearestFlag(structure.transform.position) == targetFlag) return true;
        }
        return false;
    }

    // Live precondition, not a lifetime/cumulative hire counter -- if
    // you've fired people back below 10, this simply isn't satisfied yet,
    // same as any other not-currently-satisfiable recipe in this project
    // (VILLAGE_FLAG_PLANNING.md section 6's own explicit framing).
    private bool MeetsCityFoundingConditions()
    {
        bool hasMasterworkFlag = false;
        foreach (var flag in FindObjectsByType<VillageFlag>(FindObjectsSortMode.None))
        {
            if (flag.Tier != CraftTier.Masterwork) continue;
            hasMasterworkFlag = true;
            break;
        }
        if (!hasMasterworkFlag) return false;

        int hiredCount = 0;
        foreach (var hiring in FindObjectsByType<NPCHiring>(FindObjectsSortMode.None))
            if (hiring.IsHired) hiredCount++;

        return hiredCount >= CityFoundingRequiredHiredNpcs;
    }

    // Called by BuildScreen's Select button.
    public void ArmPiece(BuildPiece piece)
    {
        if (piece == armedPiece && existingInstanceToPlace == null) return;

        // Cancelling out of (or overwriting) an in-progress re-placement —
        // the box was already pulled out of inventory to arm this mode
        // (ArmExistingPiece's own doc comment), so abandoning it here
        // without putting it back would silently orphan it: hidden/
        // carried, in no inventory slot and not placed in the world
        // either. Restore it the same way a cancelled Backpack pickup
        // would land — straight into the main inventory.
        if (existingInstanceToPlace != null)
            RestoreExistingInstanceToInventory();

        ClearGhost();
        armedPiece = piece;
        existingInstanceToPlace = null;
        armedSocketTypes = piece != null && piece.prefab != null
            ? System.Array.ConvertAll(piece.prefab.GetComponentsInChildren<BuildSocket>(), s => s.SocketType)
            : System.Array.Empty<SocketType>();
        phase = Phase.Following;
    }

    private void RestoreExistingInstanceToInventory()
    {
        var box = existingInstanceToPlace.GetComponent<StorageBox>();
        if (box == null || inventory == null) return;

        if (inventory.Inventory.AddEquipmentItem(box.PickupItem, box))
            box.Stash();
        // If even the main inventory has no room (shouldn't normally
        // happen — it just came from there a moment ago), leave the box
        // in whatever carried/hidden state it was rather than losing
        // track of it entirely.
    }

    // Re-place an already-owned instance instead of building a fresh one
    // (2026-08-21, StorageBox pick-up-and-move — see StorageBox.cs's own
    // header comment for why this exists as a real aimed placement rather
    // than a physical "drop"). piece supplies the ghost sizing/groundOffset/
    // socket-type data (the same BuildPiece the instance was originally
    // built from); existingInstance is the real GameObject Confirm will
    // reactivate and reposition instead of instantiating a new one. The
    // caller (InventoryScreen's "Place" action) is responsible for having
    // already removed the item from wherever it was carried/stashed —
    // this method only arms the placement UI.
    public void ArmExistingPiece(BuildPiece piece, GameObject existingInstance)
    {
        if (piece == null || existingInstance == null) return;

        ClearGhost();
        armedPiece = piece;
        existingInstanceToPlace = existingInstance;
        armedSocketTypes = piece.prefab != null
            ? System.Array.ConvertAll(piece.prefab.GetComponentsInChildren<BuildSocket>(), s => s.SocketType)
            : System.Array.Empty<SocketType>();
        phase = Phase.Following;
    }

    private void Update()
    {
        if (armedPiece == null || armedPiece.prefab == null || interaction?.PlayerCamera == null)
        {
            ClearGhost();
            return;
        }

        if (phase == Phase.Following)
            ResolveFollowing();

        HandleInput();
    }

    private void ResolveFollowing()
    {
        var cam = interaction.PlayerCamera.transform;
        if (!Physics.Raycast(cam.position, cam.forward, out var hit, interactRange))
        {
            ClearGhost();
            currentValid = false;
            return;
        }

        var socket = FindNearbySocket(hit.point);
        if (socket != null)
        {
            snappedSocket = socket;
            Vector3 pos;
            Quaternion rot;

            // A Wall rising from a Foundation edge needs real per-socket
            // alignment (stand vertically, sit flush on the socket)
            // instead of Foundation's own flat-tiling offset — the case
            // this class's own panelHalfSize comment flagged as needing
            // real math once Wall/Door existed. Foundation's own edge
            // socket already sits ~0.2m below its visible top surface
            // (the slab is buried per its "1m thick, mostly buried"
            // design) — placing the wall's own WallBottom socket at that
            // exact point embeds its base slightly into the slab rather
            // than floating above it, and keeps BuildSocket's "two
            // connected sockets share the same world position" invariant
            // intact with no extra offset math.
            bool wallOntoFoundation = socket.SocketType == SocketType.FoundationEdge
                && System.Array.IndexOf(armedSocketTypes, SocketType.WallBottom) >= 0;

            // A Roof panel arming onto a Wall's own WallTop socket. The
            // panel's pitch is already baked into its mesh geometry (built
            // flat, then rotated and applied in Blender — see
            // generate_roof_panel.py) with its eave sitting exactly at the
            // model's local origin, same trick as Wall's own WallBottom.
            // That means placement only needs a yaw. Same LookRotation as
            // wallOntoFoundation below, not the mirrored/negated version —
            // confirmed empirically (RoofDirectionCheck.cs, since-deleted):
            // the Blender->glTF->Unity export flips the sign of the axis
            // the branches run along, so the panel's baked-in ridge
            // direction already lands inward, toward the building center,
            // under the *same*-sign LookRotation. The negated version was
            // tried first on hand math alone and put the ridge outside the
            // building instead.
            bool roofOntoWall = socket.SocketType == SocketType.WallTop
                && System.Array.IndexOf(armedSocketTypes, SocketType.WallTop) >= 0;

            // A Door arming onto a Door-Frame Wall's own DoorFrame socket.
            // The frame socket sits at the doorway's hinge-side bottom
            // corner, and the Door's own attach point sits at the exact
            // same corner in its local space — the same point that also
            // serves as its hinge pivot at runtime (see Door.cs), so no
            // separate pivot child is needed either.
            bool doorOntoFrame = socket.SocketType == SocketType.DoorFrame
                && System.Array.IndexOf(armedSocketTypes, SocketType.DoorFrame) >= 0;

            // A Foundation arming onto a Pole's own top-frame socket, to
            // sit elevated on stilts. Foundation's new center-bottom
            // PoleTop socket sits at the exact same local point its
            // FoundationEdge sockets already use (local origin) — so it
            // ends up "mostly buried" into the Pole's top frame the same
            // way it's already mostly buried into the ground everywhere
            // else, no new offset math, same visual convention either way.
            bool foundationOntoPole = socket.SocketType == SocketType.PoleTop
                && System.Array.IndexOf(armedSocketTypes, SocketType.PoleTop) >= 0;

            if (wallOntoFoundation || roofOntoWall || foundationOntoPole)
            {
                pos = socket.transform.position;
                rot = Quaternion.LookRotation(socket.transform.forward, Vector3.up);
            }
            else if (doorOntoFrame)
            {
                // Negated, unlike the two cases above — confirmed
                // empirically (DoorPlacementCheck.cs, since-deleted), not
                // assumed from the Roof's own fix. generate_door.py builds
                // the leaf spanning local X 0 -> doorWidth, but the
                // measured imported bounds showed it actually sits in
                // local -X after the Blender->glTF->Unity export — the
                // same per-asset export-sign surprise the Roof hit on its
                // Y axis, just on Door's X axis instead. Same-sign placed
                // the leaf's bounds centered 1.15m off the doorway's
                // actual center (outside the opening entirely); negated
                // landed it 0.05m off (dead center, matching the small
                // built-in clearance margin).
                pos = socket.transform.position;
                rot = Quaternion.LookRotation(-socket.transform.forward, Vector3.up);
            }
            else
            {
                pos = socket.transform.position + socket.transform.forward * panelHalfSize;
                rot = socket.transform.root.rotation;
            }

            ShowGhost(pos, rot);
            currentValid = true;
        }
        else
        {
            snappedSocket = null;
            Vector3 groundPos = hit.point + Vector3.up * armedPiece.groundOffset;
            ShowGhost(groundPos, Quaternion.identity);
            currentValid = true;
        }
    }

    // Any of the armed piece's own socket types compatible with the
    // candidate, unoccupied, and within snapRadius — closest wins.
    private BuildSocket FindNearbySocket(Vector3 point)
    {
        BuildSocket best = null;
        float bestDist = snapRadius;

        foreach (var socket in FindObjectsByType<BuildSocket>(FindObjectsSortMode.None))
        {
            if (socket.Occupied) continue;

            bool compatible = false;
            foreach (var mine in armedSocketTypes)
            {
                if (socket.IsCompatibleWith(mine)) { compatible = true; break; }
            }
            if (!compatible) continue;

            float dist = Vector3.Distance(socket.transform.position, point);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = socket;
            }
        }

        return best;
    }

    private void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Right Mouse Button cancels — deliberately NOT Escape, which
        // FirstPersonController also reads the same frame to unlock the
        // cursor. Both firing together left the cursor unlocked with no
        // screen actually open, and PlayerMenuScreen's Tab guard refuses
        // to open while the cursor's already unlocked (avoids stacking on
        // another screen) — net effect, Tab appeared to do nothing.
        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (phase == Phase.Locked)
                phase = Phase.Following;
            else
                ArmPiece(null);
            return;
        }

        if (phase == Phase.Following)
        {
            if (ghost != null && currentValid && mouse.leftButton.wasPressedThisFrame)
            {
                if (snappedSocket != null)
                {
                    Confirm(ghost.transform.position, ghost.transform.rotation, snappedSocket);
                }
                else
                {
                    lockedPosition = ghost.transform.position;
                    lockedRotation = ghost.transform.rotation;
                    phase = Phase.Locked;
                }
            }
        }
        else // Locked — free placement's rotate-then-confirm step
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > scrollThreshold)
                lockedRotation *= Quaternion.Euler(0f, rotationStepDegrees, 0f);
            else if (scroll < -scrollThreshold)
                lockedRotation *= Quaternion.Euler(0f, -rotationStepDegrees, 0f);

            ShowGhost(lockedPosition, lockedRotation);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Confirm(lockedPosition, lockedRotation, null);
            }
        }
    }

    private void Confirm(Vector3 position, Quaternion rotation, BuildSocket socket)
    {
        // One-per-Flag cap (2026-08-22) -- needs the real placement
        // position, so it can only be enforced here, not in LockReason's
        // generic pre-aim check. Which existing-structure scan runs is
        // decided by which component the piece's own prefab carries,
        // rather than a second bespoke flag per structure type.
        if (armedPiece != null && armedPiece.requiresVillageFlagAndHiredNpc && armedPiece.prefab != null)
        {
            var targetFlag = FindNearestFlag(position);
            bool blocked = (armedPiece.prefab.GetComponent<VendorStall>() != null
                    && NearestFlagAlreadyHasStructure<VendorStall>(targetFlag))
                || (armedPiece.prefab.GetComponent<BankBox>() != null
                    && NearestFlagAlreadyHasStructure<BankBox>(targetFlag));

            if (blocked)
            {
                ShowMessage("The nearest Village Flag already has one of these.");
                phase = Phase.Following;
                return;
            }
        }

        GameObject real;

        if (existingInstanceToPlace != null)
        {
            // Re-placing an already-owned instance (StorageBox pick-up-
            // and-move) — no ingredient cost, no fresh Instantiate, no
            // skill-XP grant (this isn't crafting a new one). Reuses the
            // exact same GameObject the box has carried since it was
            // first built, so its PlacedPiece/SaveId/custom name all
            // survive automatically -- *if* it already had a PlacedPiece
            // to begin with. Found live 2026-08-21: a box spawned via
            // AdminSpawnScreen's Items tab (PlayerDropping.SpawnPickup,
            // no PlacedPiece attached — only its own separate Pieces-tab
            // path does that) never had one, and this branch originally
            // just assumed it did, silently repositioning a box that was
            // still invisible to SaveManager's placedPieces capture the
            // whole time. Self-heals now, same fields the fresh-build
            // branch below sets, so Place is robust regardless of how
            // the instance came to exist.
            real = existingInstanceToPlace;
            existingInstanceToPlace = null;
            if (real.GetComponent<PlacedPiece>() == null)
            {
                real.AddComponent<PlacedPiece>().Piece = armedPiece;
                real.GetComponent<SaveId>()?.GenerateIfMissing();
            }
            (real.GetComponent<IEquippable>())?.SetCarried(false, null);
            real.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            if (!HasIngredients(armedPiece))
            {
                ShowMessage("Not enough materials.");
                phase = Phase.Following;
                return;
            }

            RemoveIngredients(armedPiece);
            real = Instantiate(armedPiece.prefab, position, rotation);
            real.AddComponent<PlacedPiece>().Piece = armedPiece;
            // RequireComponent's auto-added SaveId doesn't reliably fire Reset()
            // when triggered by a runtime AddComponent call (same gotcha
            // SaveId.cs's own migration-script comment already flags) -- left
            // implicit, every placed structure's id silently stays empty and
            // SaveManager quietly never saves it. Confirmed live 2026-08-17: a
            // placed Village Flag vanished on reload with save.json showing
            // "placedPieces": [].
            real.GetComponent<SaveId>()?.GenerateIfMissing();
            skills?.GainExperience(armedPiece.trainedSkill, armedPiece.skillGain);
        }

        // City Statue Fame grant (VILLAGE_FLAG_PLANNING.md section 6) --
        // requiresCityFoundingConditions is only ever true on the Statue's
        // own piece, so it doubles as the trigger here rather than a third
        // bespoke flag.
        if (armedPiece.requiresCityFoundingConditions)
            fame?.GrantCityStatue();

        // Vendor Stall / Bank Box Fame grant (2026-08-22) -- same trigger
        // shape as City Statue's own grant above, keyed off which
        // component the placed prefab actually carries rather than a
        // third bespoke bool.
        if (real.GetComponent<VendorStall>() != null)
            fame?.GrantVendorStall();
        else if (real.GetComponent<BankBox>() != null)
            fame?.GrantBankBox();

        // Player Map reveal hook (PLAYER_MAP_PLANNING.md section 1) --
        // the one piece this doc's own Player Map section flagged as
        // "not yet wired" back when the Map itself shipped, closed here
        // now that the Flag/Statue actually exist to hook.
        if (real.GetComponent<VillageFlag>() is { } flag)
            mapExploration?.RevealCircle(position, CraftTierScale.VillageFlagRevealRadius(flag.Tier));
        else if (real.GetComponent<CityStatue>() != null)
            mapExploration?.RevealCircle(position, CityStatueRevealRadius);

        if (socket != null)
        {
            socket.Occupied = true;

            BuildSocket nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var s in real.GetComponentsInChildren<BuildSocket>())
            {
                float dist = Vector3.Distance(s.transform.position, socket.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = s; }
            }
            if (nearest != null) nearest.Occupied = true;
        }

        // NavMesh Phase 1 (2026-08-21) -- covers both branches above (a
        // fresh build and a re-placed existing instance), since either
        // one changes world geometry the navmesh needs to account for.
        NavMeshRebaker.RequestRebake();

        phase = Phase.Following;
    }

    private bool HasIngredients(BuildPiece piece)
    {
        if (piece.ingredients == null) return true;
        foreach (var ingredient in piece.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (GetAvailableCount(ingredient.item) < ingredient.count) return false;
        }
        return true;
    }

    private void RemoveIngredients(BuildPiece piece)
    {
        if (piece.ingredients == null) return;
        foreach (var ingredient in piece.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;

            int amount = ingredient.count;
            foreach (var inv in ReachableInventories())
            {
                if (amount <= 0) break;

                int have = IngredientMatching.GetCount(inv, ingredient.item);
                if (have <= 0) continue;

                int take = Mathf.Min(have, amount);
                IngredientMatching.Remove(inv, ingredient.item, take);
                amount -= take;
            }
        }
    }

    private void ShowGhost(Vector3 position, Quaternion rotation)
    {
        if (ghost == null)
        {
            ghost = Instantiate(armedPiece.prefab, position, rotation);
            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var s in ghost.GetComponentsInChildren<BuildSocket>())
                Destroy(s);

            if (ghostMaterial == null)
                ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.4f, 0.85f, 1f, 0.6f),
                };
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = ghostMaterial;
        }
        else
        {
            ghost.transform.SetPositionAndRotation(position, rotation);
        }
    }

    private void ClearGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        snappedSocket = null;
        currentValid = false;
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }

    // Below PlayerMagic's message (y=150) — same stacking convention.
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 420f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 190f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
