# Authentication & Distributed Tracing Guide

This guide explains how to use the authentication system and distributed tracing with span attributes in the Datadog MAUI application.

## Overview

The application now includes:
- **Token-based authentication** for API endpoints
- **Distributed tracing** with custom span attributes (including user ID)
- **RUM user context** that correlates browser sessions with authenticated users
- **Full trace correlation** between web app, API, and backend operations

## Quick Start

### 1. Start the API

```bash
cd Api
dotnet run
```

The API will start on:
- API: http://localhost:8080
- Web Portal: http://localhost:5000

### 2. Access the Web Portal

Navigate to http://localhost:5000 and you'll see the login form in the top-right corner.

**Pre-configured test users (password: "password" for all):**
- `demo` → user-001
- `admin` → user-002
- `test` → user-003

### 3. Login and Explore

1. Click "Login" with the default credentials (demo/password)
2. Your username will appear in the top-right corner
3. Click "View Profile" to see your user information
4. All subsequent API calls will include your user ID in trace spans

## Authentication Flow

### Login Process

1. **User submits credentials** via web form
2. **API validates credentials** (`POST /auth/login`)
3. **SessionManager creates trace span** with operation name `auth.login`
4. **Span tags are added**:
   - `auth.username`: The username attempting to login
   - `auth.method`: "password"
   - `auth.success`: "true" or "false"
   - `user.id`: User ID (on success)
   - `user.username`: Username (on success)
   - `user.email`: Email (on success)
   - `auth.failure_reason`: Reason for failure (if failed)

5. **Token is generated and returned**
6. **Web app stores token** in localStorage
7. **RUM user context is set** using `DD_RUM.setUser()`:
   ```javascript
   window.DD_RUM.setUser({
       id: user.userId,
       name: user.username,
       email: user.username + '@example.com'
   });
   ```

8. **RUM action is logged**:
   ```javascript
   window.DD_RUM.addAction('user_login', {
       username: currentUser.username,
       userId: currentUser.userId
   });
   ```

### Authenticated Requests

All authenticated endpoints require the `Authorization` header:

```http
Authorization: Bearer {token}
```

Example:
```bash
curl http://localhost:8080/profile \
  -H "Authorization: Bearer user-001-abc123..."
```

## API Endpoints

### Authentication Endpoints

#### POST /auth/login
Authenticate and receive a token.

**Request:**
```json
{
  "username": "demo",
  "password": "password"
}
```

**Response:**
```json
{
  "success": true,
  "token": "user-001-f3e4d2c1b0a9",
  "username": "demo",
  "userId": "user-001",
  "message": "Login successful"
}
```

**Trace Tags:**
- `auth.username`
- `auth.method`
- `auth.success`
- `user.id` (on success)
- `user.username` (on success)
- `user.email` (on success)

#### POST /auth/logout
Invalidate the current session.

**Headers:**
```
Authorization: Bearer {token}
```

**Trace Tags:**
- `logout.success`
- `user.id`

### Profile Endpoints

#### GET /profile
Retrieve the authenticated user's profile.

**Headers:**
```
Authorization: Bearer {token}
```

**Response:**
```json
{
  "userId": "user-001",
  "username": "demo",
  "email": "demo@example.com",
  "fullName": "Demo User",
  "createdAt": "2025-12-15T10:30:00Z",
  "lastLoginAt": "2026-01-14T15:45:00Z"
}
```

**Trace Tags:**
- `user.id`
- `operation.type`: "profile_fetch"
- `profile.found`
- `user.username`
- `user.email`

#### PUT /profile
Update the authenticated user's profile.

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request:**
```json
{
  "userId": "user-001",
  "username": "demo",
  "fullName": "Updated Name",
  "email": "newemail@example.com",
  "createdAt": "2025-12-15T10:30:00Z",
  "lastLoginAt": "2026-01-14T15:45:00Z"
}
```

**Trace Tags:**
- `user.id`
- `operation.type`: "profile_update"
- `update.fields`
- `update.success`
- `user.username`
- `user.email`

## Distributed Tracing Architecture

### Trace Flow

```
Web Browser (RUM)
    ↓ [HTTP Request with Correlation ID]
API Endpoint (/auth/login)
    ↓ [Calls SessionManager]
SessionManager.AuthenticateUser()
    ↓ [Creates span: auth.login]
    ↓ [Adds span tags: user.id, auth.success, etc.]
    ↓ [Returns result]
API Endpoint
    ↓ [Returns response]
Web Browser (RUM)
    ↓ [Sets RUM user context]
    ↓ [Logs RUM action]
```

### Span Attributes Example

When a user logs in, you'll see traces in Datadog APM with these attributes:

```
Span: auth.login
Resource: SessionManager.AuthenticateUser
Service: datadog-maui-api

Tags:
  - service.name: datadog-maui-api
  - auth.username: demo
  - auth.method: password
  - service.operation: user_login
  - auth.success: true
  - user.id: user-001
  - user.username: demo
  - user.email: demo@example.com
```

### Correlation Between RUM and APM

**Before Login:**
- RUM sessions have no user context
- Traces show operations but no user attribution

**After Login:**
- RUM sessions are tagged with user ID, username, and email
- All traces include `user.id` tag
- You can filter Datadog APM traces by user ID
- You can see which users encountered errors
- You can correlate frontend RUM sessions with backend API traces

## Code Examples

### Backend: Creating Traced Operations

```csharp
public LoginResponse AuthenticateUser(string username, string password)
{
    // Create a new span for this operation
    using var scope = Tracer.Instance.StartActive("auth.login");
    scope.Span.ResourceName = "SessionManager.AuthenticateUser";
    scope.Span.SetTag("service.name", "datadog-maui-api");

    // Add custom attributes
    scope.Span.SetTag("auth.username", username);
    scope.Span.SetTag("auth.method", "password");
    scope.Span.SetTag("service.operation", "user_login");

    // ... authentication logic ...

    if (success)
    {
        scope.Span.SetTag("auth.success", "true");
        scope.Span.SetTag("user.id", user.UserId);
        scope.Span.SetTag("user.username", username);
        scope.Span.SetTag("user.email", user.Email);
    }
    else
    {
        scope.Span.SetTag("auth.success", "false");
        scope.Span.SetTag("auth.failure_reason", "invalid_credentials");
    }

    return response;
}
```

### Frontend: Setting RUM User Context

```javascript
// After successful login
function setDatadogUser(user) {
    if (window.DD_RUM && user) {
        window.DD_RUM.setUser({
            id: user.userId,
            name: user.username,
            email: user.username + '@example.com'
        });

        // Optionally log the login action
        window.DD_RUM.addAction('user_login', {
            username: user.username,
            userId: user.userId
        });
    }
}

// On logout
function logout() {
    if (window.DD_RUM) {
        window.DD_RUM.clearUser();
        window.DD_RUM.addAction('user_logout');
    }
}
```

### Frontend: Making Authenticated Requests

```javascript
async function viewProfile() {
    const response = await fetch(`${API_BASE}/profile`, {
        headers: {
            'Authorization': `Bearer ${authToken}`
        }
    });

    if (response.status === 401) {
        // Session expired - logout and redirect
        logout();
        return;
    }

    const profile = await response.json();

    // Log RUM action
    if (window.DD_RUM) {
        window.DD_RUM.addAction('view_profile', {
            userId: profile.userId
        });
    }
}
```

## Datadog APM Query Examples

### Find all login attempts for a specific user

```
service:datadog-maui-api operation_name:auth.login @auth.username:demo
```

### Find failed login attempts

```
service:datadog-maui-api operation_name:auth.login @auth.success:false
```

### Find all operations by a specific user

```
service:datadog-maui-api @user.id:user-001
```

### Find profile updates

```
service:datadog-maui-api operation_name:user.update_profile @update.success:true
```

### Correlate RUM sessions with backend traces

1. Go to RUM Explorer in Datadog
2. Filter by user: `@usr.id:user-001`
3. Click on a session
4. View "Resources" tab to see API calls
5. Click "View Trace" to jump to the APM trace

## Testing Checklist

- [ ] Login with demo/password
- [ ] Verify username appears in top-right
- [ ] Open browser console and verify "Datadog RUM user set" message
- [ ] Click "View Profile" and verify profile loads
- [ ] Edit profile and save changes
- [ ] Check Datadog APM for traces with user.id tags
- [ ] Check Datadog RUM for sessions with user context
- [ ] Logout and verify RUM user context is cleared
- [ ] Try invalid credentials and verify failure is traced

## Production Considerations

### Security

⚠️ **This is a demo implementation**. For production:

1. **Use proper password hashing** (bcrypt, Argon2, etc.)
2. **Implement JWT tokens** with expiration
3. **Add HTTPS** for all communication
4. **Implement refresh tokens** for long sessions
5. **Add rate limiting** on login endpoint
6. **Validate and sanitize** all user inputs
7. **Use secure session storage** (HttpOnly cookies, not localStorage)

### Scalability

1. **Use a database** instead of in-memory storage
2. **Use Redis** or similar for session management
3. **Implement distributed session storage** for multi-instance deployments

### Monitoring

1. **Set up alerts** for failed login attempts
2. **Monitor authentication latency**
3. **Track user session duration**
4. **Alert on unusual patterns** (many failed logins, etc.)

## Troubleshooting

### RUM user context not showing

- Check browser console for errors
- Verify `DD_RUM` is defined: `console.log(window.DD_RUM)`
- Ensure login is successful before calling `setUser()`
- Check Network tab for API response

### Traces missing user.id tag

- Verify token is being sent in Authorization header
- Check that SessionManager methods are being called
- Ensure Datadog.Trace package is installed (v3.8.0+)
- Check API logs for trace creation messages

### 401 Unauthorized errors

- Token may have expired (24-hour validity)
- Logout and login again
- Check that token is stored in localStorage
- Verify Authorization header format: `Bearer {token}`

## Next Steps

1. **Add to MAUI mobile app**: Implement same auth flow in mobile app
2. **Add profile photos**: Upload and display user avatars
3. **Add roles/permissions**: Implement role-based access control
4. **Add audit logging**: Track all user actions
5. **Add password reset**: Email-based password recovery
6. **Add 2FA**: Two-factor authentication support

## Resources

- [Datadog APM Documentation](https://docs.datadoghq.com/tracing/)
- [Datadog RUM Documentation](https://docs.datadoghq.com/real_user_monitoring/)
- [Datadog .NET Tracer](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-core/)
- [RUM setUser() API](https://docs.datadoghq.com/real_user_monitoring/browser/modifying_data_and_context/?tab=npm#user-session)
