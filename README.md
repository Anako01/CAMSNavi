# CAMSNavi
Honours Project - CAMSNavi is an indoor AR navigation system developed in Unity using LiDAR scanning, SLAM and Vuforia Area Targets to provide marker-less navigation within CAMS ground floor without relying on external infrastructure

##  Project Goal

To develop an AR-based indoor navigation tool:

- **Scan** ground floor at CAMNS using LiDAR (iPad Pro) to generate accurate point cloud models.
- **Configure** scanned environments as Vuforia Area Targets for marker-less tracking.
- **Navigate** users from their current position to a selected destination within the CAMS building.
- **Visualize** the route in real time through directional AR waypoints and a 2D map overlay.

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity 2022   |
| Language | C#   |
| AR Framework | Vuforia (Area Targets)  |
| Scanning | iPad Pro LiDAR  |
| Version Control | Git + GitHub  |
| Target Platform | Android (ARCore-supported devices) |

##  How to Run

###  Requirements

- Unity 2021/2022 (matching version used for Vuforia Engine)
- Vuforia Engine package
- Android device with ARCore support
- USB cable for device deployment

###  Setup Steps

1.  **Clone the repository**
    ```bash
    git clone [https://github.com/](https://github.com/)<your-username>/CAMSNavi.git
    cd CAMSNavi
    ```
2.  Open the project in **Unity 2022**.
3.  Ensure **AR Foundation** and **Vuforia Engine package**  are installed via the Package Manager.
4.  Configure the Vuforia Area Targets (Wings 1–3) under Vuforia Engine settings.
5.  Go to **Build Settings** -> **Android** -> **Build**.
6.  Launch the app on your ARCore-supported device.

