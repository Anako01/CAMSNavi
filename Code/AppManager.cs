using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class AppManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject homePanel;
    public GameObject venuePanel;
    public GameObject endNavPanel;

    [Header("Nav Bar")]
    public Button scanTabButton;
    public Button venueTabButton;

    [Header("Home Panel UI")]
    public TMP_Text scanTitleText;
    public TMP_Text scanSubText;

    [Header("Vuforia")]
    public AreaTargetBehaviour areaTarget;

    [Header("Venue Buttons")]
    public Button labDButton;
    public Button labCButton;
    public Button classroom2Button;
    public Button n22Button;
    public Button labEButton;
    public Button bathroomButton;
    public Button endNavButton;

    [Header("Waypoints — drag Cubes in ORDER per venue")]
    public List<Transform> labDWaypoints;
    public List<Transform> labCWaypoints;
    public List<Transform> classroom2Waypoints;
    public List<Transform> n22Waypoints;
    public List<Transform> labEWaypoints;
    public List<Transform> bathroomWaypoints;

    [Header("Navigation Settings")]
    public float arrivalDistance = 1.8f;
    public Transform arCamera;

    [Header("Optional")]
    public TMP_Text feedbackText;
    public float feedbackDuration = 2.5f;

    private bool _localized      = false;
    private bool _navigating     = false;
    private bool _arrived        = false;
    private int  _currentWPIndex = 0;
    private List<Transform> _activeRoute = new List<Transform>();

    private Coroutine _feedbackCoroutine;


    void Start()
    {
        ShowHome();
        HideAllWaypoints();
        if (endNavPanel) endNavPanel.SetActive(false);
        if (feedbackText) feedbackText.gameObject.SetActive(false);

        // Nav-bar buttons
        if (scanTabButton)  scanTabButton .onClick.AddListener(ShowHome);
        if (venueTabButton) venueTabButton.onClick.AddListener(ShowVenues);

        // Venue buttons
        if (labDButton)       labDButton      .onClick.AddListener(() => StartNavigation(labDWaypoints,       "Lab D"));
        if (labCButton)       labCButton      .onClick.AddListener(() => StartNavigation(labCWaypoints,       "Lab C"));
        if (classroom2Button) classroom2Button.onClick.AddListener(() => StartNavigation(classroom2Waypoints, "Classroom 2"));
        if (n22Button)        n22Button       .onClick.AddListener(() => StartNavigation(n22Waypoints,        "N22"));
        if (labEButton)       labEButton      .onClick.AddListener(() => StartNavigation(labEWaypoints,       "Lab E"));
        if (bathroomButton)   bathroomButton  .onClick.AddListener(() => StartNavigation(bathroomWaypoints,   "Bathroom"));
        if (endNavButton)     endNavButton    .onClick.AddListener(EndNavigation);

    
        VuforiaApplication.Instance.OnVuforiaStarted += RegisterAreaTargetCallback;


        StartCoroutine(TryRegisterOnNextFrame());
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance != null)
            VuforiaApplication.Instance.OnVuforiaStarted -= RegisterAreaTargetCallback;

        if (areaTarget != null)
            areaTarget.OnTargetStatusChanged -= OnAreaTargetStatusChanged;
    }

    void Update()
    {
        if (!_navigating || _arrived) return;
        if (_activeRoute == null || _currentWPIndex >= _activeRoute.Count) return;
        if (arCamera == null) return;

        Transform target = _activeRoute[_currentWPIndex];
        if (target == null) { AdvanceWaypoint(); return; }

        float dist = Vector3.Distance(arCamera.position, target.position);
        if (dist <= arrivalDistance)
            AdvanceWaypoint();
    }

    IEnumerator TryRegisterOnNextFrame()
    {
        yield return null;
        if (areaTarget != null && areaTarget.enabled)
            RegisterAreaTargetCallback();
    }

    void RegisterAreaTargetCallback()
    {
        if (areaTarget == null)
        {
            Debug.LogError("AppManager: AreaTarget not assigned in Inspector!");
            return;
        }
        // Guard against double-subscription
        areaTarget.OnTargetStatusChanged -= OnAreaTargetStatusChanged;
        areaTarget.OnTargetStatusChanged += OnAreaTargetStatusChanged;
        Debug.Log("AppManager: Area Target callback registered.");
    }

    void OnAreaTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool tracked = status.Status == Status.TRACKED ||
                       status.Status == Status.EXTENDED_TRACKED;

        // ── FIX 2: restore current arrow if tracking recovers mid-navigation ─
        if (tracked && _navigating && !_arrived)
        {
            ShowWaypoint(_currentWPIndex);
        }

        // Only run the "first localisation" flow once
        if (_localized) return;
        if (!tracked)   return;

        _localized = true;
        Debug.Log("AppManager: Area Target localised!");

        if (scanTitleText) scanTitleText.text = "Environment localized!";
        if (scanSubText)   scanSubText.text   = "Select a venue below";

        StartCoroutine(AutoSwitchToVenues(1.2f));
    }

    IEnumerator AutoSwitchToVenues(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowVenues();
    }

    public void ShowHome()
    {
        if (homePanel)  homePanel .SetActive(true);
        if (venuePanel) venuePanel.SetActive(false);

        if (!_localized)
        {
            if (scanTitleText) scanTitleText.text = "Scan area to localize";
            if (scanSubText)   scanSubText.text   = "Point camera at your surroundings to locate";
        }
    }

    public void ShowVenues()
    {
        if (homePanel)   homePanel  .SetActive(false);
        if (venuePanel)  venuePanel .SetActive(true);
        if (endNavPanel) endNavPanel.SetActive(false);
    }

    void StartNavigation(List<Transform> route, string venueName)
    {

        if (route == null || route.Count == 0)
        {
            Debug.LogWarning($"AppManager: No waypoints assigned for '{venueName}'!");
            ShowFeedback($"'{venueName}' navigation\ncoming soon!");
            return;
        }

        StopNavigation();
        _activeRoute    = route;
        _currentWPIndex = 0;
        _navigating     = true;
        _arrived        = false;

        HideAllWaypoints();
        ShowWaypoint(0);

        // Hide venue panel and end-nav panel while navigating
        if (venuePanel)  venuePanel .SetActive(false);
        if (endNavPanel) endNavPanel.SetActive(false);

        Debug.Log($"AppManager: Navigation started → {venueName} ({route.Count} waypoints)");
    }

    void AdvanceWaypoint()
    {
        HideWaypoint(_currentWPIndex);
        _currentWPIndex++;

        if (_currentWPIndex >= _activeRoute.Count)
        {
            _arrived    = true;
            _navigating = false;
            if (endNavPanel) endNavPanel.SetActive(true);
            Debug.Log("AppManager: Destination reached!");
        }
        else
        {
            ShowWaypoint(_currentWPIndex);
        }
    }

    void ShowWaypoint(int i)
    {
        if (i < _activeRoute.Count && _activeRoute[i] != null)
            _activeRoute[i].gameObject.SetActive(true);
    }

    void HideWaypoint(int i)
    {
        if (i < _activeRoute.Count && _activeRoute[i] != null)
            _activeRoute[i].gameObject.SetActive(false);
    }

    void HideAllWaypoints()
    {
        HideList(labDWaypoints);
        HideList(labCWaypoints);
        HideList(classroom2Waypoints);
        HideList(n22Waypoints);
        HideList(labEWaypoints);
        HideList(bathroomWaypoints);
    }

    void HideList(List<Transform> list)
    {
        if (list == null) return;
        foreach (var t in list)
            if (t != null) t.gameObject.SetActive(false);
    }

    void StopNavigation()
    {
        _navigating     = false;
        _arrived        = false;
        _currentWPIndex = 0;

        if (_activeRoute != null) HideList(_activeRoute);
        _activeRoute = null;
    }

    public void EndNavigation()
    {
        StopNavigation();
        if (endNavPanel) endNavPanel.SetActive(false);

        // Allow re-localisation from scratch
        _localized = false;
        if (scanTitleText) scanTitleText.text = "Scan area to localize";
        if (scanSubText)   scanSubText.text   = "Point camera at your surroundings to locate";

        ShowHome();
    }

    // Optional on-screen feedback helper

    void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(FeedbackRoutine(message));
    }

    IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }
}
