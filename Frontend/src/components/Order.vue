<script setup>
import api from '@/api';
import { ref, computed, onMounted } from 'vue'

// Dữ liệu đơn hàng
const orders = ref([]);

const tabs = [
    { label: 'Tất cả', value: 'all', icon: 'fas fa-list', badge: 'all' },
    { label: 'Chờ xác nhận', value: 'pending', icon: 'fas fa-clock', badge: 'warning' },
    { label: 'Đang làm', value: 'preparing', icon: 'fas fa-clock', badge: 'info' },
    { label: 'Đang giao', value: 'delivering', icon: 'fas fa-shipping-fast', badge: 'info' },
    { label: 'Đã giao', value: 'completed', icon: 'fas fa-check-circle', badge: 'success' },
    { label: 'Đã hủy', value: 'cancelled', icon: 'fas fa-times-circle', badge: 'danger' }
]
const currentTab = ref('all')
const pendingCount = computed(() => countByStatus('pending'))
const preparingCount = computed(() => countByStatus('preparing'))
const deliveringCount = computed(() => countByStatus('delivering'))
const completedCount = computed(() => countByStatus('completed'))

const filteredOrders = computed(() => {
    if (currentTab.value === 'all') return orders.value
    return orders.value.filter(o => convertStatusToText(o.status) == currentTab.value)
})

const convertStatusToText = (status) => {
    const listStatus = ['pending', 'preparing', 'delivering', 'completed', 'cancelled']
    return listStatus[status] || status;
}

const currentTabText = computed(() => {
    const tab = tabs.find(t => t.value === currentTab.value)
    return tab ? tab.label.toLowerCase() : ''
})

const countByStatus = (status) => {
    if (status === 'all') return orders.value.length
    return orders.value.filter(o => convertStatusToText(o.status) == status).length
}

const selectedOrder = ref(null)
const deliveryFee = 20000

const subTotal = computed(() => {
    if (!selectedOrder.value) return 0
    return selectedOrder.value.orderItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
})

// Hàm mở modal
const openDetail = (order) => {
    selectedOrder.value = { ...order } // clone để tránh mutate
}

// Mua lại
const reorder = () => {
    alert('Đã thêm lại toàn bộ món vào giỏ hàng!')
    selectedOrder.value = null
}

// Helper
const formatPrice = (price) => price.toLocaleString('vi-VN')
const formatDate = (date) => new Date(date).toLocaleDateString('vi-VN', { weekday: 'long', hour: '2-digit', minute: '2-digit' })

const status = ["Chờ xác nhận", "Đang chuẩn bị", "Đang giao", "Đã giao", "Đã hủy"];

const statusText = (s) => status[s]
const statusClass = (s) => `status-${s}`

onMounted(async () => {
    const id = Number(localStorage.getItem("userId"));
    const res = await api.get(`/orders/${id}`);
    orders.value = res.data;
    console.log("Order data: ", res.data);
})
</script>
<template>
    <div class="order-history-container">
        <div class="header">
            <h2 class="page-title">
                <i class="fas fa-history"></i> Lịch sử đơn hàng
            </h2>
            <div class="stats">
                <div class="stat-item pending">{{ pendingCount }} Chờ xác nhận</div>
                <div class="stat-item prearing">{{ preparingCount }} Đang làm</div>
                <div class="stat-item delivering">{{ deliveringCount }} Đang giao</div>
                <div class="stat-item completed">{{ completedCount }} Đã giao</div>
            </div>
        </div>

        <div class="tabs">
            <button v-for="tab in tabs" :key="tab.value" @click="currentTab = tab.value"
                :class="{ active: currentTab === tab.value }" class="tab-btn">
                <i :class="tab.icon"></i>
                {{ tab.label }}
                <span class="badge" :class="tab.badge">{{ countByStatus(tab.value) }}</span>
            </button>
        </div>

        <!-- Empty state -->
        <div v-if="orders.length === 0" class="empty-state">
            <i class="fas fa-receipt fa-4x"></i>
            <p>Chưa có đơn hàng nào</p>
            <router-link to="/menu" class="btn-order-now">Đặt món ngay</router-link>
        </div>

        <!-- Danh sách đơn hàng -->
        <div v-else class="orders-list">
            <div v-for="order in filteredOrders" :key="order.id" class="order-card" :class="statusClass(order.status)">
                <div class="order-header">
                    <div class="order-id">Đơn hàng #{{ order.orderId }}</div>
                    <div class="order-date">{{ formatDate(order.orderDate) }}</div>
                </div>

                <div class="order-status">
                    <span class="status-badge" :class="statusClass(order.status)">
                        {{ statusText(order.status) }}
                    </span>
                </div>

                <!-- <div class="order-items-preview">
                    <div v-for="item in order.orderItems.slice(0, 2)" :key="item.id" class="preview-item">
                        <img :src="item.fastFood?.imageUrl ?? item.combo.imageUrl" :alt="item.name" />
                        <span>x{{ item.quantity }}</span>
                    </div>
                    <span v-if="order.orderItems.length > 2" class="more-items">+{{ order.orderItems.length - 2 }} món
                        nữa</span>


                </div> -->
                <div class="order-items">
                    <div v-for="item in order.orderItems.slice(0,2)" :key="item.orderItemId" class="order-item">
                        <img :src="item.fastFood?.imageUrl ?? item.combo?.imageUrl" :alt="item.name"
                            class="item-thumb" />
                        <div class="item-info">
                            <h4>{{ item.comboName ?? item.fastFoodName }}</h4>
                            <p v-if="item.itemType == 1" class="combo-note">
                                {{ item.combo?.description}}
                            </p>
                            <span class="quantity">x{{ item.quantity }}</span>
                            <span class="price">{{ formatPrice(item.unitPrice * item.quantity) }}₫</span>
                        </div>
                        <span v-if="order.orderItems.length > 2" class="more-items">
                            +{{ order.orderItems.length - 2 }} món nữa
                        </span>
                    </div>
                </div>

                <div class="order-footer">
                    <div class="total">
                        <strong>Tổng:</strong>
                        <span class="total-price">{{ formatPrice(order.totalAmount) }}₫</span>
                    </div>
                    <button @click="openDetail(order)" class="btn-detail">Chi tiết <i
                            class="bi bi-arrow-bar-right"></i></button>
                </div>
            </div>
        </div>

        <!-- ==================== MODAL CHI TIẾT ĐƠN HÀNG (nằm trong component) ==================== -->
        <Teleport to="body">
            <div v-if="selectedOrder" class="modal-overlay" @click="selectedOrder = null"></div>

            <div v-if="selectedOrder" class="detail-modal">
                <div class="modal-header">
                    <h3>Chi tiết đơn hàng #{{ selectedOrder.id }}</h3>
                    <button @click="selectedOrder = null" class="btn-close">✕</button>
                </div>

                <div class="modal-body">
                    <!-- Trạng thái + thời gian -->
                    <div class="order-meta">
                        <div class="status-badge big" :class="statusClass(selectedOrder.status)">
                            {{ statusText(selectedOrder.status) }}
                        </div>
                        <p>Đặt lúc: {{ formatDate(selectedOrder.orderDate) }}</p>
                    </div>

                    <!-- Danh sách món đầy đủ -->
                    <div class="items-section">
                        <h4>Các món đã đặt</h4>
                        <div v-for="item in selectedOrder.orderItems" :key="item.id" class="detail-item">
                            <img :src="item.fastFood?.imageUrl ?? item.combo.imageUrl" :alt="item.name"
                                class="item-thumb" />
                            <div class="item-info">
                                <h5>{{ item.name }}</h5>
                                <p v-if="item.itemType == 1" class="combo-note">
                                    {{ item.combo.description }}
                                </p>
                                <div class="qty-price">
                                    <span class="quantity">x{{ item.quantity }}</span>
                                    <span class="price">{{ formatPrice(item.unitPrice) }}₫</span>
                                </div>
                            </div>
                            <div class="item-total">{{ formatPrice(item.unitPrice * item.quantity) }}₫</div>
                        </div>
                    </div>

                    <!-- Tổng kết -->
                    <div class="summary-section">
                        <div class="row"><span>Tạm tính</span><span>{{ formatPrice(subTotal) }}₫</span></div>
                        <div class="row"><span>Phí giao hàng</span><span>{{ formatPrice(deliveryFee) }}₫</span></div>
                        <div class="row total">
                            <strong>Tổng cộng</strong>
                            <strong>{{ formatPrice(selectedOrder.totalAmount) }}₫</strong>
                        </div>
                    </div>

                    <!-- Thông tin giao hàng -->
                    <div class="delivery-section">
                        <h4>Thông tin nhận hàng</h4>
                        <p><strong>Người nhận:</strong> {{ selectedOrder.customerName ?? selectedOrder.user.fullName }}
                        </p>
                        <p><strong>SĐT:</strong> {{ selectedOrder.phone }}</p>
                        <p><strong>Địa chỉ:</strong> {{ selectedOrder.address }}</p>
                        <p v-if="selectedOrder.note"><strong>Ghi chú:</strong> {{ selectedOrder.note }}</p>
                    </div>

                    <!-- Nút hành động -->
                    <div class="modal-actions">
                        <button v-if="selectedOrder.status === 'delivered'" @click="reorder" class="btn-reorder">
                            <i class="fas fa-redo"></i> Mua lại đơn này
                        </button>
                        <button class="btn-close-modal" @click="selectedOrder = null">Đóng</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>
<style scoped>
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
.order-history-container {
    padding: 20px;
    max-width: 1200px;
    margin: 0 auto;
    background: #f5f5f5;
    min-height: 100vh;
}

.page-title {
    text-align: center;
    color: #2c3e50;
    margin-bottom: 30px;
    font-size: 26px;
}

.empty-state {
    text-align: center;
    padding: 80px 20px;
    color: #999;
}

.empty-state i {
    color: #ddd;
    margin-bottom: 20px;
}

.btn-order-now {
    display: inline-block;
    margin-top: 20px;
    padding: 12px 30px;
    background: #2874f0;
    color: white;
    border-radius: 30px;
    text-decoration: none;
    font-weight: bold;
}

.orders-list {
    display: flex;
    flex-direction: column;
    gap: 20px;
}

.order-card {
    background: white;
    border-radius: 16px;
    overflow: hidden;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.08);
    transition: all 0.3s;
}

.order-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 12px 30px rgba(0, 0, 0, 0.12);
}

.order-header {
    background: #2c3e50;
    color: white;
    padding: 14px 20px;
    display: flex;
    justify-content: space-between;
    font-size: 15px;
}

.order-id {
    font-weight: bold;
}

.order-status {
    padding: 12px 20px;
    background: #f8f9fa;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 15px;
}

.status-badge {
    padding: 6px 14px;
    border-radius: 20px;
    font-weight: bold;
    font-size: 13px;
}

.status-1, .status-0 {
    background: #fff3cd;
    color: #856404;
}

.status-2 {
    background: #d1ecf1;
    color: #0c5460;
}

.status-3 {
    background: #d4edda;
    color: #155724;
}

.status-4 {
    background: #f8d7da;
    color: #721c24;
}

.tracking {
    color: #e67e22;
    font-weight: bold;
}

.order-items {
    background: white;
    padding: 10px 0;
}

.order-item  {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 12px 20px;
    border-bottom: 1px solid rgba(0, 0, 0, 0.256);
}

.order-item:last-child {
    border-bottom: none;
}

.order-items-preview{
    padding: 10px;
    background-color: white;
    
}

.preview-item{
    display: flex;
    gap: 10px;
    margin-top: 3px;
    width: 100%;
    padding: 10px 16px;
    border-bottom: 1px solid rgba(0, 0, 0, 0.138);
    background-color: white;
}
.preview-item img{
    width: 80px;
    height: 70px;
    object-fit: cover;
}

.item-thumb {
    width: 60px;
    height: 60px;
    object-fit: cover;
    border-radius: 10px;
}

.item-info h4 {
    margin: 0 0 4px;
    font-size: 15px;
    color: #2c3e50;
}

.combo-note {
    font-size: 12px;
    color: #2874f0;
    margin: 4px 0;
}

.quantity {
    background: #eee;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 12px;
    margin-right: 8px;
}

.price {
    font-weight: bold;
    color: #e67e22;
}

.order-footer {
    padding: 16px 20px;
    background: #f8f9fa;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.total-price {
    font-size: 20px;
    color: #e74c3c;
    margin-left: 8px;
}

.actions {
    display: flex;
    gap: 10px;
}

.actions button,
.actions a {
    padding: 8px 16px;
    border-radius: 8px;
    font-size: 14px;
    cursor: pointer;
    text-decoration: none;
    display: flex;
    align-items: center;
    gap: 6px;
}

.btn-reorder {
    background: #2874f0;
    color: white;
    border: none;
}

.btn-track {
    background: #fff;
    color: #e67e22;
    border: 1px solid #e67e22;
}

.btn-detail {
    border: none;
    padding: 3px 7px;
    border-radius: 5px;
    background: #d8caca;
    color: #333;
    font-size: 16px;
    font-weight: 600;
}

.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.6);
    z-index: 999;
    backdrop-filter: blur(8px);
}

.detail-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 90%;
    max-width: 660px;
    max-height: 90vh;
    background: white;
    border-radius: 20px;
    overflow: hidden;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    z-index: 1000;
    animation: modalPop 0.4s ease;
}

@keyframes modalPop {
    from {
        transform: translate(-50%, -50%) scale(0.8);
        opacity: 0;
    }

    to {
        transform: translate(-50%, -50%) scale(1);
        opacity: 1;
    }
}

.modal-header {
    background: #2c3e50;
    color: white;
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.modal-header h3 {
    margin: 0;
    font-size: 20px;
}

.btn-close {
    background: none;
    border: none;
    color: white;
    font-size: 24px;
    cursor: pointer;
}

.modal-body {
    padding: 20px;
    overflow-y: auto;
    max-height: 70vh;
}

.order-meta {
    text-align: center;
    margin-bottom: 20px;
}

.status-badge.big {
    font-size: 16px;
    padding: 10px 20px;
    border-radius: 30px;
}

.items-section h4,
.delivery-section h4 {
    margin: 24px 0 12px;
    color: #2c3e50;
    border-bottom: 1px solid #eee;
    padding-bottom: 8px;
}

.detail-item {
    display: flex;
    align-items: center;
    gap: 14px;
    padding: 12px 0;
    border-bottom: 1px dashed #eee;
}

.detail-item .item-thumb {
    width: 70px;
    height: 70px;
    object-fit: cover;
    border-radius: 12px;
}

.item-info h5 {
    margin: 0 0 4px;
    font-size: 16px;
}

.qty-price {
    display: flex;
    justify-content: space-between;
    margin-top: 6px;
}

.item-total {
    margin-left: auto;
    font-weight: bold;
    color: #e67e22;
}

.summary-section {
    background: #f8f9fa;
    padding: 16px;
    border-radius: 12px;
    margin: 20px 0;
}

.summary-section .row {
    display: flex;
    justify-content: space-between;
    margin: 8px 0;
}

.summary-section .total {
    font-size: 20px;
    font-weight: bold;
    color: #e74c3c;
    padding-top: 12px;
    border-top: 2px solid #ddd;
}

.modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 24px;
}

.btn-reorder {
    flex: 1;
    background: #2874f0;
    color: white;
    border: none;
    padding: 14px;
    border-radius: 12px;
    font-weight: bold;
}

.btn-close-modal {
    flex: 1;
    background: #eee;
    border: none;
    padding: 14px;
    border-radius: 12px;
    cursor: pointer;
    font-weight: 600;
    transition: all 0.2s;
}
.btn-close-modal:hover{
    background-color: #d14f13cf;
    color: white;
}

/* Responsive */
@media (max-width: 600px) {

    .order-header,
    .order-footer {
        flex-direction: column;
        align-items: flex-start;
        gap: 8px;
    }

    .actions {
        width: 100%;
        justify-content: stretch;
    }

    .actions button,
    .actions a {
        flex: 1;
        justify-content: center;
    }
}
</style>