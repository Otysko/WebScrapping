# WebScrapping

**This application automates the process of logging into Binance, handling various security checks, and gathering specific trader data using PuppeteerSharp. It's designed for scalable deployment on Kubernetes.**

## Features

*   **Automated Binance Login:**
    *   Handles username/password authentication.
    *   Solves moving captchas.
    *   Solves 9-picture image selection captchas.
    *   Automates 2FA code submission from email.
*   **Data Gathering:** Gathers informations about opened trades for specific traders and stores this information in database once logged in.
*   **PuppeteerSharp Integration:** Leverages PuppeteerSharp for robust browser automation.
*   **Kubernetes Ready:** Designed to run as a containerized application in a Kubernetes cluster.
*   **Scalable Replicas:** Supports running multiple instances (replicas) for parallel processing or different configurations.
*   **Dynamic Configuration via JobController:** Each replica can receive specific tasks or configurations (e.g., which trader to monitor) from an external "JobController" application.

## How It Works (High-Level Architecture)

This application uses PuppeteerSharp to control a headless Chromium browser.

1.  **Login Sequence:**
    *   Navigates to the Binance login page.
    *   Inputs user credentials (username/password).
    *   **Captcha Handling:** Implements logic to detect and attempt to solve:
        *   Moving slider captchas.
        *   9-picture grid captchas.
    *   **2FA Email Authentication:**
        *   Monitors a specified email account (via GMail API) for the 2FA code.
        *   Extracts the code and submits it on the Binance page.
2.  **Data Gathering:**
    *   Once logged in, the application navigates to the relevant sections of Binance.
    *   It receives instructions from the JobController.
    *   It then scrapes the required data related to that instruction.
3.  **JobController Interaction:**
    *   Each instance/replica of this application is expected to communicate with a "JobController" service.
    *   The JobController assigns specific tasks or configurations (target trader IDs) to each replica.
    *   Replicas gets job via a MQ from JobController

## Prerequisites

*   .NET SDK 8.0
*   A Binance account with standard login credentials.
*   Access to an email account for 2FA code retrieval (Using GMail API).
*   The "JobController" application/service running and accessible. (Not included code here)
*   AWS account with enabled access to Amazon Rekognition Client (For handling captcha).
*   (Optional) Database for storing different user credentials (For running several replicas).
*   (Optional) Docker installed for containerization.
*   (Optional) Kubernetes cluster (e.g., Minikube, kind, or a cloud provider's K8s service) for deployment.