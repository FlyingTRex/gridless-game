using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WildHarvest
{

public class PatchController : MonoBehaviour
{
    [SerializeField] private List<GameObject> growthStages;
    [SerializeField] private GameObject bunch;
    [SerializeField] private GameObject leafParticles;
    [SerializeField] private int harvestStage = 4;
    [SerializeField] private int harvestAmount = 5;
    [SerializeField] private bool mouseDownTrigger = true;
    [SerializeField] private bool timerTrigger = true;
    [SerializeField] private float growthTimerInterval = 5f;
    [SerializeField] private float initialDelay = 0f;

    [Header("Input")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float maxDistance = 100f;

    private float t = 0.0f;
    private int listIndex;
    private Animator plantAnimator;
    private bool isHovered;

    private void Start()
    {
        StartCoroutine(GrowthTimer());
        plantAnimator = GetComponentInChildren<Animator>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera == null) return;

        // Read pointer position from whichever input system is active
        Vector2 pointerPos;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return;
        pointerPos = Mouse.current.position.ReadValue();
#else
        pointerPos = Input.mousePosition;
#endif

        Ray ray = mainCamera.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.IsChildOf(transform))
            {
                // Hover highlight
                if (!isHovered)
                {
                    isHovered = true;
                    SetEmission(Color.white * 0.2f);
                }

                // Click to grow
                bool clicked = false;

#if ENABLE_INPUT_SYSTEM
                clicked = Mouse.current.leftButton.wasPressedThisFrame;
#else
                clicked = Input.GetMouseButtonDown(0);
#endif

                if (clicked && mouseDownTrigger)
                {
                    PatchGrowth();
                }
            }
            else
            {
                ClearHover();
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void SetEmission(Color color)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            material.SetColor("_Emission", color);
        }
    }

    private void ClearHover()
    {
        if (isHovered)
        {
            isHovered = false;
            SetEmission(Color.black);
        }
    }

    private IEnumerator GrowthTimer()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (timerTrigger)
            {
                t += Time.deltaTime;
                if (t >= growthTimerInterval)
                {
                    PatchGrowth();
                    t = 0.0f;
                }
            }
            yield return null;
        }
    }

    public void PatchGrowth()
    {
        // Play plant reaction animation
        plantAnimator.Play("React");

        // Swaps out to new mesh
        growthStages[listIndex].SetActive(false);
        listIndex = (listIndex + 1) % growthStages.Count;
        growthStages[listIndex].SetActive(true);

        // Call harvest function
        if (listIndex == harvestStage)
        {
            Harvest();
        }
    }

    public void Harvest()
    {
        // Reset Leaf Particles.
        leafParticles.SetActive(false);

        // Spawn bunches
        for (int i = 0; i < harvestAmount; i++)
        {
            Vector3 objectPosition = transform.position;
            Instantiate(bunch, objectPosition, Quaternion.Euler(0, Random.Range(0, 360), 0));
        }

        // Spawn Leaf Particles
        leafParticles.SetActive(true);
    }
}
}
