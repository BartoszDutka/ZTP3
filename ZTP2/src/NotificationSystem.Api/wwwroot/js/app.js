// Notification System - Frontend Application
const API_BASE = '/api/notifications';

// DOM Elements
const createForm = document.getElementById('createForm');
const refreshBtn = document.getElementById('refreshBtn');
const notificationsBody = document.getElementById('notificationsBody');
const filterStatus = document.getElementById('filterStatus');
const filterChannel = document.getElementById('filterChannel');
const detailsModal = document.getElementById('detailsModal');
const detailsContent = document.getElementById('detailsContent');
const toastContainer = document.getElementById('toastContainer');

// Stats elements
const statTotal = document.getElementById('statTotal');
const statPending = document.getElementById('statPending');
const statSent = document.getElementById('statSent');
const statFailed = document.getElementById('statFailed');

// State
let allNotifications = [];

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    setDefaultScheduledDate();
    loadNotifications();
    setupEventListeners();
});

function setDefaultScheduledDate() {
    const scheduledAtInput = document.getElementById('scheduledAt');
    const now = new Date();
    now.setMinutes(now.getMinutes() + 5); // Default: 5 minutes from now
    scheduledAtInput.value = formatDateTimeLocal(now);
}

function formatDateTimeLocal(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
}

function setupEventListeners() {
    // Form submission
    createForm.addEventListener('submit', handleCreateNotification);
    
    // Refresh button
    refreshBtn.addEventListener('click', loadNotifications);
    
    // Filters
    filterStatus.addEventListener('change', renderNotifications);
    filterChannel.addEventListener('change', renderNotifications);
    
    // Modal close
    document.querySelector('.close').addEventListener('click', closeModal);
    window.addEventListener('click', (e) => {
        if (e.target === detailsModal) closeModal();
    });
    
    // Auto-refresh every 30 seconds
    setInterval(loadNotifications, 30000);
}

// API Functions
async function loadNotifications() {
    try {
        showLoading();
        const response = await fetch(API_BASE);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        allNotifications = await response.json();
        renderNotifications();
        updateStats();
    } catch (error) {
        console.error('Error loading notifications:', error);
        showError('Błąd podczas ładowania powiadomień');
        notificationsBody.innerHTML = `
            <tr>
                <td colspan="7" class="empty-state">
                    <div class="empty-state-icon">⚠️</div>
                    <div class="empty-state-text">Błąd połączenia z serwerem</div>
                </td>
            </tr>
        `;
    }
}

async function handleCreateNotification(e) {
    e.preventDefault();
    
    const formData = new FormData(createForm);
    const scheduledAt = new Date(formData.get('scheduledAt')).toISOString();
    
    const notification = {
        content: formData.get('content'),
        channel: formData.get('channel'),
        recipient: formData.get('recipient'),
        timeZone: formData.get('timeZone'),
        scheduledAt: scheduledAt,
        priority: formData.get('priority') ? parseInt(formData.get('priority')) : null
    };
    
    try {
        const response = await fetch(API_BASE, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(notification)
        });
        
        if (!response.ok) {
            const error = await response.text();
            throw new Error(error);
        }
        
        showToast('Powiadomienie zostało utworzone!', 'success');
        createForm.reset();
        setDefaultScheduledDate();
        loadNotifications();
    } catch (error) {
        console.error('Error creating notification:', error);
        showToast(`Błąd: ${error.message}`, 'error');
    }
}

async function cancelNotification(id) {
    if (!confirm('Czy na pewno chcesz anulować to powiadomienie?')) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/${id}/cancel`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            const error = await response.text();
            throw new Error(error);
        }
        
        showToast('Powiadomienie zostało anulowane', 'info');
        loadNotifications();
    } catch (error) {
        console.error('Error cancelling notification:', error);
        showToast(`Błąd: ${error.message}`, 'error');
    }
}

async function viewDetails(id) {
    const notification = allNotifications.find(n => n.id === id);
    
    if (!notification) {
        showToast('Nie znaleziono powiadomienia', 'error');
        return;
    }
    
    detailsContent.innerHTML = `
        <div class="detail-row">
            <span class="detail-label">ID:</span>
            <span class="detail-value"><code>${notification.id}</code></span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Status:</span>
            <span class="detail-value">${getStatusBadge(notification.status)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Kanał:</span>
            <span class="detail-value">${getChannelBadge(notification.channel)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Odbiorca:</span>
            <span class="detail-value">${escapeHtml(notification.recipient)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Treść:</span>
            <span class="detail-value">${escapeHtml(notification.content)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Strefa czasowa:</span>
            <span class="detail-value">${notification.timeZone}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Zaplanowane na:</span>
            <span class="detail-value">${formatDate(notification.scheduledAt)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Priorytet:</span>
            <span class="detail-value">${notification.priority ?? 'Brak'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Utworzono:</span>
            <span class="detail-value">${formatDate(notification.createdAt)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Wysłano:</span>
            <span class="detail-value">${notification.sentAt ? formatDate(notification.sentAt) : 'Nie wysłano'}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Liczba prób:</span>
            <span class="detail-value">${notification.retryCount}</span>
        </div>
        ${notification.lastError ? `
        <div class="detail-row">
            <span class="detail-label">Ostatni błąd:</span>
            <span class="detail-value" style="color: var(--danger-color);">${escapeHtml(notification.lastError)}</span>
        </div>
        ` : ''}
    `;
    
    detailsModal.style.display = 'block';
}

function closeModal() {
    detailsModal.style.display = 'none';
}

// Rendering Functions
function renderNotifications() {
    const statusFilter = filterStatus.value;
    const channelFilter = filterChannel.value;
    
    let filtered = allNotifications;
    
    if (statusFilter) {
        filtered = filtered.filter(n => n.status === statusFilter);
    }
    
    if (channelFilter) {
        filtered = filtered.filter(n => n.channel === channelFilter);
    }
    
    // Sort by scheduledAt descending
    filtered.sort((a, b) => new Date(b.scheduledAt) - new Date(a.scheduledAt));
    
    if (filtered.length === 0) {
        notificationsBody.innerHTML = `
            <tr>
                <td colspan="7" class="empty-state">
                    <div class="empty-state-icon">📭</div>
                    <div class="empty-state-text">Brak powiadomień</div>
                </td>
            </tr>
        `;
        return;
    }
    
    notificationsBody.innerHTML = filtered.map(notification => `
        <tr>
            <td>${getStatusBadge(notification.status)}</td>
            <td>${getChannelBadge(notification.channel)}</td>
            <td class="truncate" title="${escapeHtml(notification.recipient)}">${escapeHtml(notification.recipient)}</td>
            <td class="truncate" title="${escapeHtml(notification.content)}">${escapeHtml(notification.content)}</td>
            <td>${formatDate(notification.scheduledAt)}</td>
            <td>${notification.priority ?? '-'}</td>
            <td class="actions">
                <button class="btn btn-info" onclick="viewDetails('${notification.id}')">👁️</button>
                ${canCancel(notification.status) ? 
                    `<button class="btn btn-danger" onclick="cancelNotification('${notification.id}')">🚫</button>` : 
                    ''
                }
            </td>
        </tr>
    `).join('');
}

function showLoading() {
    notificationsBody.innerHTML = `
        <tr>
            <td colspan="7" class="loading">
                <div class="spinner"></div> Ładowanie...
            </td>
        </tr>
    `;
}

function updateStats() {
    statTotal.textContent = allNotifications.length;
    statPending.textContent = allNotifications.filter(n => 
        n.status === 'Pending' || n.status === 'Scheduled' || n.status === 'Sending'
    ).length;
    statSent.textContent = allNotifications.filter(n => n.status === 'Sent').length;
    statFailed.textContent = allNotifications.filter(n => n.status === 'Failed').length;
}

// Helper Functions
function getStatusBadge(status) {
    const statusMap = {
        'Pending': { class: 'status-pending', icon: '⏳' },
        'Scheduled': { class: 'status-scheduled', icon: '📅' },
        'Sending': { class: 'status-sending', icon: '📤' },
        'Sent': { class: 'status-sent', icon: '✅' },
        'Failed': { class: 'status-failed', icon: '❌' },
        'Cancelled': { class: 'status-cancelled', icon: '🚫' }
    };
    
    const info = statusMap[status] || { class: '', icon: '' };
    return `<span class="status-badge ${info.class}">${info.icon} ${status}</span>`;
}

function getChannelBadge(channel) {
    const channelMap = {
        'Email': { class: 'channel-email', icon: '📧' },
        'Push': { class: 'channel-push', icon: '📱' }
    };
    
    const info = channelMap[channel] || { class: '', icon: '' };
    return `<span class="channel-badge ${info.class}">${info.icon} ${channel}</span>`;
}

function formatDate(dateString) {
    if (!dateString) return '-';
    
    const date = new Date(dateString);
    return date.toLocaleString('pl-PL', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    });
}

function canCancel(status) {
    return status === 'Pending' || status === 'Scheduled';
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    
    toastContainer.appendChild(toast);
    
    setTimeout(() => {
        toast.remove();
    }, 3000);
}

function showError(message) {
    showToast(message, 'error');
}
