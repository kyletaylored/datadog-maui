### **1. Project Overview**

- **Goal:** Create a cross-platform mobile application (Android/iOS) that allows users to submit basic data, view external web content, and track interaction telemetry via a robust backend API.
- **Architecture Strategy:** "Local-First" development. The API will be containerized (Docker) immediately to allow the mobile emulator to communicate with it as if it were a remote server, minimizing friction when moving to the cloud later.

---

### **2. Functional Requirements**

#### **A. Mobile Application (.NET MAUI)**

- **Target Platforms:** Android (min SDK 26 recommended) and iOS (latest 2 major versions).
- **UI Structure (Shell Navigation):**
- **Tab 1: Dashboard/Input:**
- **Input Fields:** Two text entry fields (e.g., "Session Name", "Notes") and one numeric field.
- **Action:** A "Submit" button that validates inputs and POSTs data to the API.
- **Feedback:** Toast notification or alert upon success/failure.

- **Tab 2: Web Portal:**
- **Component:** A generic `WebView` control.
- **Function:** Loads a hardcoded URL (e.g., project documentation or a reporting dashboard) with a pull-to-refresh feature.

- **Telemetry & Tracking:**
- The app must generate a unique `CorrelationID` for every API request to allow end-to-end tracing.
- Log basic events (App Start, Form Submitted, Tab Changed).

#### **B. Backend API (.NET 8/9 Web API)**

- **Endpoints:**
- `GET /health`: Returns 200 OK (for container health checks).
- `POST /data`: Accepts the JSON payload from the mobile app inputs.
- `GET /config`: Returns dynamic configuration (e.g., feature flags or the URL for the WebView).

- **Data Handling:**
- In-memory data store (e.g., `ConcurrentDictionary` or generic List) for the MVP phase to avoid setting up a database immediately.
- Console logging for all received requests (structured logging is preferred).

- **Containerization:**
- Must include a `Dockerfile` optimized for ASP.NET Core.
- Must run on a fixed local port (e.g., 5000/5001) to ensure the Android Emulator can reach it.

---

### **3. Technical Architecture & Constraints**

- **Frameworks:**
- **App:** .NET MAUI (.NET 8 or 9)
- **API:** ASP.NET Core Web API (.NET 8 or 9)
- **Container:** Docker Desktop or Podman

- **Connectivity (Crucial for Local MVP):**
- **Android Emulator:** Must access the local API via the special alias `10.0.2.2` (which maps to `localhost` on your machine).
- **iOS Simulator:** Can access `localhost` directly.
- **Requirement:** The `HttpClient` service in the MAUI app must detect the platform and swap the base URL dynamically (`10.0.2.2` for Android, `localhost` for iOS).

---

### **4. Project Phases (The Plan)**

#### **Phase 1: The "Hollow" Shell**

- **Objective:** Get the app running on both simulators with UI only.
- **Tasks:**
- Initialize .NET MAUI solution.
- Build the `AppShell` with two tabs.
- Implement the `WebView` on Tab 2.
- Create the UI layout for inputs on Tab 1 (Buttons do nothing yet).

#### **Phase 2: The Containerized API**

- **Objective:** API running in Docker and accessible via `curl`.
- **Tasks:**
- Create .NET Web API project.
- Implement `POST /data` endpoint with logging.
- Create `Dockerfile`.
- Run container: `docker run -p 5000:8080 my-mvp-api`.
- Test via Postman/Curl.

#### **Phase 3: Integration & "Wiring"**

- **Objective:** App talks to Container.
- **Tasks:**
- Create a `ApiService` class in MAUI.
- Implement the `BaseUrl` logic (handling the Android `10.0.2.2` nuance).
- Wire the "Submit" button to the `ApiService`.
- **Verification:** Submit data on Android emulator -> See log appear in Docker container terminal.
