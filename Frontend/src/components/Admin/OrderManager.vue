<script setup>
import api from '@/api'
import { AlertSuccess } from '@/Notification'
import { ref, computed, onMounted } from 'vue'

const orders = ref([])

const currentTab = ref('all')
const selectedOrder = ref(null)

const tabs = [
    { label: 'Tất cả', value: 'all', icon: 'fas fa-list', badge: 'all' },
    { label: 'Chờ xử lý', value: 'pending', icon: 'fas fa-clock', badge: 'warning' },
    { label: 'Đang làm', value: 'preparing', icon: 'fas fa-clock', badge: 'info' },
    { label: 'Đang giao', value: 'delivering', icon: 'fas fa-shipping-fast', badge: 'info' },
    { label: 'Đã giao', value: 'completed', icon: 'fas fa-check-circle', badge: 'success' },
    { label: 'Đã hủy', value: 'cancelled', icon: 'fas fa-times-circle', badge: 'danger' }
]

const filteredOrders = computed(() => {
    if (currentTab.value === 'all') return orders.value
    return orders.value.filter(o => convertStatusToText(o.status) == currentTab.value)
})

const countByStatus = (status) => {
    if (status === 'all') return orders.value.length
    return orders.value.filter(o => convertStatusToText(o.status) == status).length
}

const pendingCount = computed(() => countByStatus('pending'))
const preparingCount = computed(() => countByStatus('preparing'))
const deliveringCount = computed(() => countByStatus('delivering'))
const completedCount = computed(() => countByStatus('completed'))

const currentTabText = computed(() => {
    const tab = tabs.find(t => t.value === currentTab.value)
    return tab ? tab.label.toLowerCase() : ''
})

const convertStatusToText = (status) => {
    const listStatus = ['pending', 'preparing', 'delivering', 'completed', 'cancelled']
    return listStatus[status] || status;
}

const getStatusText = (status) => {
    const map = { "pending": 'Chờ xử lý', "preparing": 'Đang làm', "delivering": 'Đang giao', "completed": 'Đã giao', "cancelled": 'Đã hủy' }
    return map[convertStatusToText(status)] || convertStatusToText(status)
}



const getStatusClass = (status) => `status-${convertStatusToText(status)}`

const formatPrice = (price) => price.toLocaleString('vi-VN')
const formatTime = (date) => new Date(date).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' })

const openDetail = (order) => selectedOrder.value = { ...order }
const updateStatus = async (order, newStatus) => {
    await api.put(`/orders/${order.orderId}`, { Status:newStatus });
    order.status = newStatus
    AlertSuccess(`Đơn hàng #${order.orderId} đã chuyển sang: ${getStatusText(newStatus)}`)
}

onMounted(async() => {
    const res = await api.get("/orders");
    orders.value = res.data;
    console.log("Order data: ", res.data);
})

</script>
<template>
    <div class="order-manager">
        <div class="header">
            <h2>Quản lý đơn hàng</h2>
            <div class="stats">
                <div class="stat-item pending">{{ pendingCount }} Chờ xử lý</div>
                <div class="stat-item prearing">{{ preparingCount }} Đang làm</div>
                <div class="stat-item delivering">{{ deliveringCount }} Đang giao</div>
                <div class="stat-item completed">{{ completedCount }} Đã giao</div>
            </div>
        </div>

        <!-- Tab trạng thái -->
        <div class="tabs">
            <button v-for="tab in tabs" :key="tab.value" @click="currentTab = tab.value"
                :class="{ active: currentTab === tab.value }" class="tab-btn">
                <i :class="tab.icon"></i>
                {{ tab.label }}
                <span class="badge" :class="tab.badge">{{ countByStatus(tab.value) }}</span>
            </button>
        </div>

        <!-- Danh sách đơn hàng -->
        <div class="order-container">
            <div class="orders-list">
                <div v-if="filteredOrders.length === 0" class="empty-state">
                    <i class="fas fa-receipt fa-4x"></i>
                    <p>Không có đơn hàng {{ currentTabText }}</p>
                </div>

                <div v-else v-for="order in filteredOrders" :key="order.id" class="order-card">
                    <div class="order-header">
                        <div class="order-id">#{{ order.orderId }}</div>
                        <div class="order-time">{{ formatTime(order.orderDate) }}</div>
                    </div>

                    <div class="customer-info">
                        <div>
                            <strong>{{ order.customerName || order.user.username }}</strong>
                            <span class="phone">{{ order.phone }}</span>
                        </div>
                        <span class="status-badge" :class="getStatusClass(order.status)">
                            {{ getStatusText(order.status) }}
                        </span>
                    </div>

                    <div class="order-items">
                        <div v-for="item in order.orderItems" :key="item.orderItemId" class="item">
                            <span class="quantity">x{{ item.quantity }}</span>
                            {{ item.comboName ?? item.fastFoodName }}
                            <span v-if="item.itemType == 1" class="combo-tag">Combo</span>
                        </div>
                    </div>

                    <div class="order-footer">
                        <div class="total">
                            <strong>Tổng:</strong> {{ formatPrice(order.totalAmount) }}₫
                        </div>
                        <div class="actions">
                            <button @click="openDetail(order)" class="btn-detail">Chi tiết</button>
                            <button v-if="convertStatusToText(order.status) == 'pending'"
                                @click="updateStatus(order, 'preparing')" class="btn-action preparing">Bắt đầu
                                làm</button>
                            <button v-if="convertStatusToText(order.status) == 'preparing'"
                                @click="updateStatus(order, 'delivering')" class="btn-action delivering">Giao
                                hàng</button>
                            <button v-if="convertStatusToText(order.status) == 'delivering'"
                                @click="updateStatus(order, 'completed')" class="btn-action completed">Hoàn
                                thành</button>
                            <button
                                v-if="[convertStatusToText(0), convertStatusToText(1)].includes(convertStatusToText(order.status))"
                                @click="updateStatus(order, 'cancelled')" class="btn-cancel">Hủy đơn</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Modal chi tiết đơn hàng -->
        <Teleport to="body">
            <div v-if="selectedOrder" class="modal-overlay" @click="selectedOrder = null"></div>
            <div v-if="selectedOrder" class="detail-modal">
                <div class="modal-header">
                    <h3>Chi tiết đơn hàng #{{ selectedOrder.orderId }}</h3>
                    <button @click="selectedOrder = null" class="btn-close"></button>
                </div>
                <div class="modal-body">
                    <div class="info-row">
                        <strong>Khách hàng:</strong> {{ selectedOrder.user.username }} ({{ selectedOrder.phone }})
                    </div>
                    <div class="info-row">
                        <strong>Địa chỉ:</strong> {{ selectedOrder.address }}
                    </div>
                    <div class="info-row" v-if="selectedOrder.notes">
                        <strong>Ghi chú:</strong> {{ selectedOrder.notes }}
                    </div>
                    <div class="items-list">
                        <h4>Các món:</h4>
                        <div v-for="item in selectedOrder.orderItems" :key="item.orderItemId" class="detail-item">
                            <div>
                                <strong>x{{ item.quantity }}</strong> {{ item.comboName || item.fastFoodName }}
                                <span v-if="item.itemType == 1" class="combo">Combo</span>
                            </div>
                            <div>{{ formatPrice(item.unitPrice * item.quantity) }}₫</div>
                        </div>
                    </div>
                    <div class="total-row">
                        <strong>Tổng cộng:</strong> {{ formatPrice(selectedOrder.totalAmount) }}₫
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>
<style scoped>
.order-manager {
    padding: 24px;
    background: #f5f7fa;
    min-height: 100vh;
}

.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
}

.header h2 {
    margin: 0;
    color: #2c3e50;
}


.tabs {
    display: flex;
    gap: 12px;
    margin-bottom: 24px;
    flex-wrap: wrap;
}

.tab-btn {
    padding: 12px 20px;
    border: none;
    border-radius: 12px;
    background: white;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    transition: all 0.3s;
}

.tab-btn.active {
    background: #2874f0;
    color: white;
}

.badge {
    margin-left: 8px;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 12px;
}
.badge.all{
    background-color: #07de39c7;
}

.badge.warning {
    background: #fff3cd;
    color: #856404;
}

.badge.info {
    background: #d1ecf1;
    color: #0c5460;
}

.badge.success {
    background: #d4edda;
    color: #155724;
}

.badge.danger {
    background: #f8d7da;
    color: #721c24;
}

.order-container{
    max-height: 600px;
    overflow-y: auto;
}

.orders-list {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.order-card {
    background: white;
    border-radius: 16px;
    overflow: hidden;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
}

.order-header {
    background: #2c3e50;
    color: white;
    padding: 14px 20px;
    display: flex;
    justify-content: space-between;
}

.customer-info {
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #f8f9fa;
}

.phone {
    margin-left: 10px;
    color: #666;
}

.status-badge {
    padding: 6px 14px;
    border-radius: 20px;
    font-size: 13px;
    font-weight: bold;
}

.status-pending {
    background: #fff3cd;
    color: #856404;
}

.status-preparing {
    background: #d4e6fc;
    color: #1e40af;
}

.status-delivering {
    background: #d1ecf1;
    color: #0c5460;
}

.status-completed {
    background: #d4edda;
    color: #155724;
}

.status-cancelled {
    background: #f8d7da;
    color: #721c24;
}

.order-items {
    padding: 12px 20px;
}

.item {
    display: flex;
    align-items: center;
    gap: 8px;
    margin: 8px 0;
}

.quantity {
    background: #eee;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 12px;
}

.combo-tag {
    background: #2874f0;
    color: white;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 10px;
}

.order-footer {
    padding: 16px 20px;
    background: #f8f9fa;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.total {
    font-size: 18px;
}

.actions {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.btn-detail,
.btn-action,
.btn-cancel {
    padding: 8px 16px;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 13px;
}

.btn-detail {
    background: #3498db;
    color: white;
}

.btn-action.preparing {
    background: #3498db;
    color: white;
}

.btn-action.delivering {
    background: #e67e22;
    color: white;
}

.btn-action.completed {
    background: #27ae60;
    color: white;
}

.btn-cancel {
    background: #e74c3c;
    color: white;
}

/* Modal */
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.6);
    z-index: 999;
}

.detail-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 90%;
    max-width: 600px;
    max-height: 90vh;
    background: white;
    border-radius: 16px;
    overflow: hidden;
    z-index: 1000;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
    background: #2c3e50;
    color: white;
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.btn-close {
    background: none;
    border: none;
    color: white;
    font-size: 28px;
    cursor: pointer;
}

.modal-body {
    padding: 24px;
    overflow-y: auto;
}

.info-row {
    margin: 12px 0;
}

.items-list h4 {
    margin: 20px 0 12px;
    border-bottom: 1px solid #eee;
    padding-bottom: 8px;
}

.detail-item {
    display: flex;
    justify-content: space-between;
    padding: 8px 0;
}

.combo {
    background: #2874f0;
    color: white;
    padding: 2px 6px;
    border-radius: 8px;
    font-size: 11px;
    margin-left: 8px;
}

.total-row {
    margin-top: 20px;
    font-size: 20px;
    font-weight: bold;
    text-align: right;
    color: #e74c3c;
}
</style>