const API_BASE = window.location.origin;
let authToken = localStorage.getItem('authToken');
let currentUser = JSON.parse(localStorage.getItem('currentUser') || 'null');
let cart = [];

// Initialize auth state on page load
function initAuth() {
    if (authToken && currentUser) {
        showUserInfo(currentUser);
        setDatadogUser(currentUser);
    }
}

// Generate correlation ID for requests
function generateCorrelationId() {
    return 'web-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
}

// Login
async function login() {
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;

    if (!username || !password) {
        alert('Please enter username and password');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });

        const data = await response.json();

        if (response.ok && data.success) {
            // Store auth info
            authToken = data.token;
            currentUser = {
                userId: data.userId,
                username: data.username
            };
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('currentUser', JSON.stringify(currentUser));

            // Update UI
            showUserInfo(currentUser);

            // Set Datadog RUM user context
            setDatadogUser(currentUser);

            // Log success action in RUM
            if (window.DD_RUM) {
                window.DD_RUM.addAction('user_login', {
                    username: currentUser.username,
                    userId: currentUser.userId
                });
            }

            console.log('✅ Login successful:', currentUser);
        } else {
            alert(data.message || 'Login failed');
        }
    } catch (error) {
        alert('Login error: ' + error.message);
        console.error('Login error:', error);
    }
}

// Logout
async function logout() {
    try {
        if (authToken) {
            await fetch(`${API_BASE}/auth/logout`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${authToken}`
                }
            });
        }

        // Clear local storage
        authToken = null;
        currentUser = null;
        localStorage.removeItem('authToken');
        localStorage.removeItem('currentUser');

        // Clear Datadog RUM user
        if (window.DD_RUM) {
            window.DD_RUM.clearUser();
            window.DD_RUM.addAction('user_logout');
        }

        // Update UI
        showLoginForm();
        hideProfile();

        console.log('✅ Logout successful');
    } catch (error) {
        console.error('Logout error:', error);
    }
}

// Set Datadog RUM user context
function setDatadogUser(user) {
    if (window.DD_RUM && user) {
        const rumUser = {
            id: user.userId,
            name: user.username
        };

        // Add email if available (prefer actual email over generated one)
        if (user.email) {
            rumUser.email = user.email;
        } else {
            rumUser.email = user.username + '@example.com';
        }

        // Add full name if available as custom attribute
        if (user.fullName) {
            rumUser.fullName = user.fullName;
        }

        window.DD_RUM.setUser(rumUser);
        console.log('✅ Datadog RUM user set:', rumUser);
    }
}

// Show user info
function showUserInfo(user) {
    document.getElementById('login-form-container').style.display = 'none';
    document.getElementById('user-info-container').style.display = 'block';
    document.getElementById('user-name').textContent = '👋 ' + user.username;
    document.getElementById('user-id').textContent = 'User ID: ' + user.userId;
}

// Show login form
function showLoginForm() {
    document.getElementById('login-form-container').style.display = 'flex';
    document.getElementById('user-info-container').style.display = 'none';
    document.getElementById('login-username').value = 'demo';
    document.getElementById('login-password').value = 'password';
}

// View profile
async function viewProfile() {
    if (!authToken) {
        alert('Please login first');
        return;
    }

    const profileCard = document.getElementById('profile-card');
    const profileContent = document.getElementById('profile-content');
    profileContent.innerHTML = '<span class="status loading">Loading profile...</span>';
    profileCard.style.display = 'block';

    try {
        const response = await fetch(`${API_BASE}/profile`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (response.status === 401) {
            alert('Session expired. Please login again.');
            logout();
            return;
        }

        const profile = await response.json();

        if (response.ok) {
            profileContent.innerHTML = `
                <div class="config-display">
                    <strong>User ID:</strong> ${profile.userId}<br>
                    <strong>Username:</strong> ${profile.username}<br>
                    <strong>Email:</strong> ${profile.email}<br>
                    <strong>Full Name:</strong> ${profile.fullName}<br>
                    <strong>Created:</strong> ${new Date(profile.createdAt).toLocaleString()}<br>
                    <strong>Last Login:</strong> ${profile.lastLoginAt ? new Date(profile.lastLoginAt).toLocaleString() : 'Never'}
                </div>
                <button onclick="editProfile('${profile.userId}', '${profile.fullName}', '${profile.email}')" style="margin-top: 15px;">Edit Profile</button>
                <button class="secondary" onclick="hideProfile()" style="margin-top: 15px;">Close</button>
            `;

            // Log RUM action
            if (window.DD_RUM) {
                window.DD_RUM.addAction('view_profile', {
                    userId: profile.userId
                });
            }
        } else {
            throw new Error('Failed to load profile');
        }
    } catch (error) {
        profileContent.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Edit profile
function editProfile(userId, currentFullName, currentEmail) {
    const newFullName = prompt('Enter new full name:', currentFullName);
    if (newFullName === null) return;

    const newEmail = prompt('Enter new email:', currentEmail);
    if (newEmail === null) return;

    updateProfile(userId, newFullName, newEmail);
}

// Update profile
async function updateProfile(userId, fullName, email) {
    if (!authToken) {
        alert('Please login first');
        return;
    }

    const profileContent = document.getElementById('profile-content');
    profileContent.innerHTML = '<span class="status loading">Updating profile...</span>';

    try {
        const response = await fetch(`${API_BASE}/profile`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userId: userId,
                username: currentUser.username,
                email: email,
                fullName: fullName,
                createdAt: new Date().toISOString(),
                lastLoginAt: new Date().toISOString()
            })
        });

        if (response.status === 401) {
            alert('Session expired. Please login again.');
            logout();
            return;
        }

        const result = await response.json();

        if (response.ok) {
            alert('Profile updated successfully!');

            // Update Datadog RUM user context with new profile data
            if (window.DD_RUM) {
                window.DD_RUM.addAction('update_profile', {
                    userId: userId,
                    email: email,
                    fullName: fullName
                });

                // Update RUM user with the new profile information
                setDatadogUser({
                    userId: userId,
                    username: currentUser.username,
                    email: email,
                    fullName: fullName
                });
            }

            // Refresh profile display
            viewProfile();
        } else {
            throw new Error(result.message || 'Failed to update profile');
        }
    } catch (error) {
        profileContent.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Hide profile
function hideProfile() {
    document.getElementById('profile-card').style.display = 'none';
}

// Health Check
async function checkHealth() {
    const statusDiv = document.getElementById('health-status');
    statusDiv.innerHTML = '<span class="status loading">Checking...</span>';

    try {
        const response = await fetch(`${API_BASE}/health`);
        const data = await response.json();

        if (response.ok) {
            statusDiv.innerHTML = `
                <span class="status healthy">✓ Healthy</span>
                <div class="response-box">
                    <strong>Status:</strong> ${data.status}<br>
                    <strong>Timestamp:</strong> ${new Date(data.timestamp).toLocaleString()}
                </div>
            `;
        } else {
            throw new Error('Health check failed');
        }
    } catch (error) {
        statusDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Get Configuration
async function getConfig() {
    const displayDiv = document.getElementById('config-display');
    displayDiv.innerHTML = '<span class="status loading">Loading...</span>';

    try {
        const correlationId = generateCorrelationId();
        const response = await fetch(`${API_BASE}/config`, {
            headers: {
                'X-Correlation-ID': correlationId
            }
        });
        const data = await response.json();

        if (response.ok) {
            displayDiv.innerHTML = `
                <div class="config-display">
                    <strong>WebView URL:</strong> <a href="${data.webViewUrl}" target="_blank">${data.webViewUrl}</a><br>
                    <strong>Feature Flags:</strong>
                    <pre>${JSON.stringify(data.featureFlags, null, 2)}</pre>
                </div>
                <div class="trace-info">
                    <strong>Correlation ID:</strong> ${correlationId}
                </div>
            `;
        } else {
            throw new Error('Failed to load config');
        }
    } catch (error) {
        displayDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Submit Data
async function submitData(event) {
    event.preventDefault();

    const responseDiv = document.getElementById('submit-response');
    responseDiv.innerHTML = '<span class="status loading">Submitting...</span>';

    const sessionName = document.getElementById('session-name').value;
    const notes = document.getElementById('notes').value;
    const numericValue = parseFloat(document.getElementById('numeric-value').value);
    const correlationId = generateCorrelationId();

    try {
        const response = await fetch(`${API_BASE}/data`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                correlationId: correlationId,
                sessionName: sessionName,
                notes: notes,
                numericValue: numericValue
            })
        });

        const data = await response.json();

        if (response.ok) {
            responseDiv.innerHTML = `
                <span class="status healthy">✓ Success</span>
                <div class="response-box">
                    <strong>Message:</strong> ${data.message}<br>
                    <strong>Timestamp:</strong> ${new Date(data.timestamp).toLocaleString()}
                </div>
                <div class="trace-info">
                    <strong>Correlation ID:</strong> ${data.correlationId}<br>
                    <strong>Trace ID:</strong> ${data.traceId}<br>
                    <strong>Span ID:</strong> ${data.spanId}
                </div>
            `;

            // Clear form
            document.getElementById('submit-form').reset();

            // Auto-refresh data list
            loadData();
        } else {
            throw new Error('Submission failed');
        }
    } catch (error) {
        responseDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Load Data
async function loadData() {
    const listDiv = document.getElementById('data-list');
    listDiv.innerHTML = '<span class="status loading">Loading...</span>';

    try {
        const response = await fetch(`${API_BASE}/data`);
        const data = await response.json();

        if (response.ok) {
            if (data.length === 0) {
                listDiv.innerHTML = '<div class="empty-state">No data submitted yet. Submit some data above!</div>';
            } else {
                listDiv.innerHTML = '<div class="data-list">' +
                    data.map(item => `
                        <div class="data-item">
                            <strong>Session:</strong> ${item.sessionName}<br>
                            <strong>Notes:</strong> ${item.notes || '(none)'}<br>
                            <strong>Value:</strong> ${item.numericValue}<br>
                            <strong>Correlation ID:</strong> ${item.correlationId}<br>
                            <span class="timestamp">${new Date(item.timestamp).toLocaleString()}</span>
                        </div>
                    `).join('') +
                '</div>';
            }
        } else {
            throw new Error('Failed to load data');
        }
    } catch (error) {
        listDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Clear Data Display
function clearDataDisplay() {
    document.getElementById('data-list').innerHTML = '<div class="empty-state">Click "Refresh Data" to load submissions</div>';
}

// ===== COMMERCE APIs =====

// Load Products
async function loadProducts() {
    const listDiv = document.getElementById('products-list');
    listDiv.innerHTML = '<span class="status loading">Loading products...</span>';

    const category = document.getElementById('category-filter').value;
    const limit = document.getElementById('product-limit').value;
    const sort = document.getElementById('product-sort').value;

    try {
        let url = `${API_BASE}/products`;
        const params = new URLSearchParams();

        if (limit) params.append('limit', limit);
        if (sort) params.append('sort', sort);

        if (category) {
            url = `${API_BASE}/products/category/${category}`;
        }

        const queryString = params.toString();
        if (queryString) url += '?' + queryString;

        const response = await fetch(url);
        const products = await response.json();

        if (response.ok) {
            if (products.length === 0) {
                listDiv.innerHTML = '<div class="empty-state">No products found</div>';
            } else {
                listDiv.innerHTML = '<div class="products-grid">' +
                    products.map(product => `
                        <div class="product-card">
                            <div class="product-category">${product.category}</div>
                            <h3>${product.title}</h3>
                            <p class="product-price">$${product.price.toFixed(2)}</p>
                            <p class="product-desc">${product.description}</p>
                            <button onclick="addToCart(${product.id}, '${product.title.replace(/'/g, "\\'")}', ${product.price})">Add to Cart</button>
                        </div>
                    `).join('') +
                '</div>';
            }
        } else {
            throw new Error('Failed to load products');
        }
    } catch (error) {
        listDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Load Categories
async function loadCategories() {
    try {
        const response = await fetch(`${API_BASE}/products/categories`);
        const categories = await response.json();

        if (response.ok) {
            const select = document.getElementById('category-filter');
            select.innerHTML = '<option value="">All Categories</option>' +
                categories.map(cat => `<option value="${cat}">${cat.charAt(0).toUpperCase() + cat.slice(1)}</option>`).join('');
        }
    } catch (error) {
        console.error('Failed to load categories:', error);
    }
}

// Add to Cart
function addToCart(productId, title, price) {
    const existing = cart.find(item => item.productId === productId);

    if (existing) {
        existing.quantity++;
    } else {
        cart.push({ productId, title, price, quantity: 1 });
    }

    updateCartDisplay();

    // Log to Datadog RUM
    if (window.DD_RUM) {
        window.DD_RUM.addAction('add_to_cart', {
            productId: productId,
            title: title,
            price: price
        });
    }

    console.log('Added to cart:', title);
}

// Remove from Cart
function removeFromCart(productId) {
    cart = cart.filter(item => item.productId !== productId);
    updateCartDisplay();
}

// Update Cart Quantity
function updateCartQuantity(productId, newQuantity) {
    const item = cart.find(item => item.productId === productId);
    if (item) {
        if (newQuantity <= 0) {
            removeFromCart(productId);
        } else {
            item.quantity = parseInt(newQuantity);
            updateCartDisplay();
        }
    }
}

// Update Cart Display
function updateCartDisplay() {
    const cartDiv = document.getElementById('cart-items');
    const totalDiv = document.getElementById('cart-total');

    if (cart.length === 0) {
        cartDiv.innerHTML = '<div class="empty-state">Your cart is empty</div>';
        totalDiv.innerHTML = '';
        return;
    }

    const total = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);

    cartDiv.innerHTML = cart.map(item => `
        <div class="cart-item">
            <div class="cart-item-info">
                <strong>${item.title}</strong><br>
                <span>$${item.price.toFixed(2)} × ${item.quantity} = $${(item.price * item.quantity).toFixed(2)}</span>
            </div>
            <div class="cart-item-actions">
                <input type="number" value="${item.quantity}" min="1" style="width: 60px;"
                    onchange="updateCartQuantity(${item.productId}, this.value)">
                <button class="secondary" onclick="removeFromCart(${item.productId})">Remove</button>
            </div>
        </div>
    `).join('');

    totalDiv.innerHTML = `
        <div class="cart-total-line">
            <strong>Total:</strong> <span class="cart-total-amount">$${total.toFixed(2)}</span>
        </div>
        <button onclick="checkout()" style="width: 100%; margin-top: 10px;">Checkout</button>
        <button class="secondary" onclick="clearCart()" style="width: 100%; margin-top: 5px;">Clear Cart</button>
    `;
}

// Clear Cart
function clearCart() {
    if (confirm('Clear all items from cart?')) {
        cart = [];
        updateCartDisplay();
    }
}

// Checkout
async function checkout() {
    if (cart.length === 0) {
        alert('Your cart is empty!');
        return;
    }

    if (!currentUser) {
        alert('Please login first to checkout');
        return;
    }

    const cartDiv = document.getElementById('cart-items');
    cartDiv.innerHTML = '<span class="status loading">Processing checkout...</span>';

    try {
        const cartData = {
            userId: currentUser.userId,
            date: new Date().toISOString(),
            products: cart.map(item => ({
                productId: item.productId,
                quantity: item.quantity
            }))
        };

        const response = await fetch(`${API_BASE}/carts`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(cartData)
        });

        const result = await response.json();

        if (response.ok) {
            // Log to Datadog RUM
            if (window.DD_RUM) {
                window.DD_RUM.addAction('checkout', {
                    cartId: result.id,
                    userId: currentUser.userId,
                    itemCount: cart.length,
                    total: cart.reduce((sum, item) => sum + (item.price * item.quantity), 0)
                });
            }

            alert(`Order placed successfully! Cart ID: ${result.id}`);
            cart = [];
            updateCartDisplay();
            loadOrders();
        } else {
            throw new Error('Checkout failed');
        }
    } catch (error) {
        cartDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
        setTimeout(() => updateCartDisplay(), 2000);
    }
}

// Load Orders (User's Carts)
async function loadOrders() {
    if (!currentUser) {
        document.getElementById('orders-list').innerHTML = '<div class="empty-state">Please login to view orders</div>';
        return;
    }

    const listDiv = document.getElementById('orders-list');
    listDiv.innerHTML = '<span class="status loading">Loading orders...</span>';

    try {
        const response = await fetch(`${API_BASE}/carts/user/${currentUser.userId}`);
        const orders = await response.json();

        if (response.ok) {
            if (orders.length === 0) {
                listDiv.innerHTML = '<div class="empty-state">No orders yet. Add items to cart and checkout!</div>';
            } else {
                listDiv.innerHTML = '<div class="orders-list">' +
                    orders.map(order => `
                        <div class="order-card">
                            <div class="order-header">
                                <strong>Order #${order.id}</strong>
                                <span class="timestamp">${new Date(order.date).toLocaleString()}</span>
                            </div>
                            <div class="order-items">
                                ${order.products.map(p => `
                                    <div>Product ID: ${p.productId} × ${p.quantity}</div>
                                `).join('')}
                            </div>
                        </div>
                    `).join('') +
                '</div>';
            }
        } else {
            throw new Error('Failed to load orders');
        }
    } catch (error) {
        listDiv.innerHTML = `<span class="status error">✗ Error: ${error.message}</span>`;
    }
}

// Auto-check health and load initial data on page load
window.addEventListener('load', () => {
    initAuth();
    checkHealth();
    loadCategories();
    loadProducts();
    loadOrders();
    console.log('%c🐕 Datadog MAUI Web Portal (.NET Framework)', 'font-size: 20px; color: #667eea; font-weight: bold;');
    console.log('API Base URL:', API_BASE);
    console.log('Authentication:', authToken ? '✅ Logged in as ' + currentUser.username : '❌ Not logged in');
    console.log('Ready for testing!');
});
