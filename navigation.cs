using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class navigation : MonoBehaviour
{
    [System.Serializable]
    public struct VenueRoute
    {
        public string venueID;
        public GameObject pathGroup;
    }

    [Header("UI Panels")]
    public GameObject homePagePanel;
    public GameObject venuePanel;
    public GameObject navigationPanel;
    public GameObject aboutPanel;

    [Header("End Navigation Button")]
    public GameObject endNavButton;

    [Header("All Area Targets (Wings)")]
    [Tooltip("Drag wing1, wing2, and wing3 here from your hierarchy")]
    public List<AreaTargetBehaviour> areaTargets;

    [Header("All Venue Routes")]
    public List<VenueRoute> venueRoutes;

    private GameObject currentActivePath;
    private bool _localized = false;

    void Start()
    {
        ShowPanel(homePagePanel);
        HideAllPaths();

        if (endNavButton != null) endNavButton.SetActive(false);

        VuforiaApplication.Instance.OnVuforiaStarted += RegisterAllAreaTargets;
        StartCoroutine(TryRegisterOnNextFrame());
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance != null)
            VuforiaApplication.Instance.OnVuforiaStarted -= RegisterAllAreaTargets;

        UnregisterAllAreaTargets();
    }

    IEnumerator TryRegisterOnNextFrame()
    {
        yield return null;
        RegisterAllAreaTargets();
    }

    private void RegisterAllAreaTargets()
    {
        if (areaTargets == null || areaTargets.Count == 0)
        {
            Debug.LogError("CAMSNAVI: No Area Targets assigned!");
            return;
        }

        foreach (var at in areaTargets)
        {
            if (at != null)
            {
                at.OnTargetStatusChanged -= OnAreaTargetStatusChanged;
                at.OnTargetStatusChanged += OnAreaTargetStatusChanged;
                Debug.Log($"CAMSNAVI: Registered listener on '{at.TargetName}'");
            }
        }
    }

    private void UnregisterAllAreaTargets()
    {
        if (areaTargets == null) return;
        foreach (var at in areaTargets)
        {
            if (at != null) at.OnTargetStatusChanged -= OnAreaTargetStatusChanged;
        }
    }

    private void OnAreaTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;

        Debug.Log($"CAMSNAVI: [{behaviour.TargetName}] status → {status.Status} | tracked={tracked} | _localized={_localized}");

        if (tracked && currentActivePath != null)
            currentActivePath.SetActive(true);

        if (_localized || !tracked) return;

        _localized = true;
        Debug.Log($"CAMSNAVI: Localised via '{behaviour.TargetName}'");
        StartCoroutine(AutoSwitchToVenues(1.0f));
    }

    IEnumerator AutoSwitchToVenues(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPanel(venuePanel);
    }

    public void SelectDestination(string destinationID)
    {
        Debug.Log($"CAMSNAVI: SelectDestination called with '{destinationID}'");

        if (currentActivePath != null)
        {
            currentActivePath.SetActive(false);
            currentActivePath = null;
        }

        bool routeFound = false;
        foreach (var route in venueRoutes)
        {
            if (route.venueID == destinationID)
            {
                if (route.pathGroup != null)
                {
                    route.pathGroup.SetActive(true);
                    currentActivePath = route.pathGroup;
                    routeFound = true;
                    Debug.Log($"CAMSNAVI: Route '{destinationID}' activated.");
                }
                else
                {
                    Debug.LogError($"CAMSNAVI: Route '{destinationID}' pathGroup is null!");
                }
                break;
            }
        }

        if (!routeFound)
        {
            Debug.LogWarning($"CAMSNAVI: No route found for '{destinationID}'. Check venueID strings match exactly.");
            return;
        }

        if (endNavButton != null) endNavButton.SetActive(true);

        ShowPanel(navigationPanel);
    }

    public void EndNavigation()
    {
        Debug.Log("CAMSNAVI: EndNavigation called.");

        if (currentActivePath != null)
        {
            currentActivePath.SetActive(false);
            currentActivePath = null;
        }

        if (endNavButton != null) endNavButton.SetActive(false);

        // Reset so re-localisation can happen
        _localized = false;

        ShowPanel(homePagePanel);

        // Force re-check wings that are already tracked right now
        StartCoroutine(CheckAlreadyTrackedTargets());
    }

    // After EndNavigation, if a wing is already tracked in background,
    // the status changed event won't fire again — so we check manually
    IEnumerator CheckAlreadyTrackedTargets()
    {
        yield return new WaitForSeconds(0.5f);

        if (_localized) yield break; // already re-localised, nothing to do

        foreach (var at in areaTargets)
        {
            if (at == null) continue;

            bool tracked = at.TargetStatus.Status == Status.TRACKED ||
                           at.TargetStatus.Status == Status.EXTENDED_TRACKED;

            if (tracked)
            {
                _localized = true;
                Debug.Log($"CAMSNAVI: Re-localised via already tracked '{at.TargetName}'");
                StartCoroutine(AutoSwitchToVenues(1.0f));
                break;
            }
        }

        // If nothing tracked yet, OnTargetStatusChanged will handle it
        // when the user points the camera at a wing
    }

    public void OpenAboutPanel()
    {
        ShowPanel(aboutPanel);
    }

    public void CloseAboutPanel()
    {
        if (currentActivePath != null)
            ShowPanel(navigationPanel);
        else if (_localized)
            ShowPanel(venuePanel);
        else
            ShowPanel(homePagePanel);
    }

    private void HideAllPaths()
    {
        foreach (var route in venueRoutes)
        {
            if (route.pathGroup != null) route.pathGroup.SetActive(false);
        }
    }

    private void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow == null)
        {
            Debug.LogError("CAMSNAVI: ShowPanel received a null panel!");
            return;
        }

        homePagePanel.SetActive(panelToShow == homePagePanel);
        venuePanel.SetActive(panelToShow == venuePanel);
        navigationPanel.SetActive(panelToShow == navigationPanel);
        if (aboutPanel != null) aboutPanel.SetActive(panelToShow == aboutPanel);
    }
}
