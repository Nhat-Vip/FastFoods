<script setup>
import api from '@/api'
import { AlertSuccess } from '@/Notification'
import { ref, computed, onMounted } from 'vue'

const orders = ref([])

const currentTab = ref('all')
const selectedOrder = ref(null)

const currentPage = ref(1);
const itemsPerPage = ref(6);
const totalPages = computed(() => {
    return Array.from({ length: Math.ceil(orders.value.length / itemsPerPage.value) }, (_, i) => i + 1);
});
const maxPageIndex = ref(5);
const minPageIndex = computed(() => {
    const half = Math.floor(maxPageIndex.value / 2);
    let start = currentPage.value - half;

    if (start < 1) start = 1;

    if (start + maxPageIndex.value - 1 > totalPages.value.length) {
        start = Math.max(1, totalPages.value.length - maxPageIndex.value + 1);
    }

    return start;
});

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
const convertStatusToTextVietNamese = (status) => {
    const listStatus = ['Chờ xử lý', 'Đang làm', 'Đang giao', 'Đã giao', 'Đã hủy']
    return listStatus[status] || status;
}


const getStatusText = (status) => {
    const map = { "pending": 'Chờ xử lý', "preparing": 'Đang làm', "delivering": 'Đang giao', "completed": 'Đã giao', "cancelled": 'Đã hủy' }
    return map[convertStatusToText(status)] || convertStatusToText(status)
}



const getStatusClass = (status) => `status-${convertStatusToText(status)}`

const formatPrice = (price) => price.toLocaleString('vi-VN')
const formatTime = (date) => new Date(date).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' })

const openDetail = (event, order) => {
    if(event.target.closest('.btn-action') || event.target.closest('.btn-cancel') ) return;
    selectedOrder.value = { ...order }
}
const updateStatus = async (order, newStatus) => {
    await api.put(`/orders/${order.orderId}`, { Status: newStatus });
    order.status = newStatus
    AlertSuccess(`Đơn hàng #${order.orderId} đã chuyển sang: ${getStatusText(newStatus)}`)
}

onMounted(async () => {
    const res = await api.get("/orders");
    orders.value = res.data;
    console.log("Order data: ", res.data);
})

</script>
<template>
    <div class="order-manager">
        <div class="header">
            <h2>Quản lý đơn hàng</h2>
        </div>

        <!-- Tab trạng thái -->
        <div class="tabs">
            <button v-for="tab in tabs" :key="tab.value" @click="currentTab = tab.value"
                :class="{ active: currentTab === tab.value }" class="tab-btn">
                <i :class="tab.icon"></i>
                {{ tab.label }}
                <!-- <span class="badge" :class="tab.badge">{{ countByStatus(tab.value) }}</span> -->
            </button>
        </div>
        <div class="w-100 text-center fs-4" v-if="filteredOrders.length === 0">
            <p>Không có đơn hàng {{ currentTabText }}</p>
        </div>
        <!-- Bảng danh sách -->
        <div class="table-container" v-if="filteredOrders.length > 0">
            <table class="user-table">
                <thead>
                    <tr>
                        <th class="text-center">#</th>
                        <!-- <th>Tên đăng nhập</th> -->
                        <th>Tên người đặt</th>
                        <!-- <th>Địa chỉ</th> -->
                        <th>SĐT</th>
                        <!-- <th>ghi chú</th> -->
                        <th>Trạng thái</th>
                        <!-- <th>Ngày tạo</th> -->
                        <th class="text-center">Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="order in filteredOrders" :key="order.orderId" @click="openDetail($event, order)">
                        <td class="text-center">{{ order.orderId }}</td>
                        <!-- <td><strong>{{ user.username }}</strong></td> -->
                        <td>
                            <div class="user-info">
                                {{ order.customerName }}
                            </div>
                        </td>
                        <!-- <td>{{ order.address }}</td> -->
                        <td>{{ order.phone }}</td>
                        <!-- <td>{{ order.note }}</td> -->
                        <td class="status">
                            <span :class="'status-'+convertStatusToText(order.status)"></span>
                            <span class="status-badge">
                                {{ convertStatusToTextVietNamese(order.status) }}
                            </span>
                        </td>
                        <!-- <td>{{ formatDate(user.createdAt) }}</td> -->
                        <td class="actions">
                            <button v-if="convertStatusToText(order.status) == 'pending'"
                                @click="updateStatus(order, '1')" class="btn-action preparing">Bắt đầu
                                làm</button>
                            <button v-if="convertStatusToText(order.status) == 'preparing'"
                                @click="updateStatus(order, '2')" class="btn-action delivering">Giao
                                hàng</button>
                            <button v-if="convertStatusToText(order.status) == 'delivering'"
                                @click="updateStatus(order, '3')" class="btn-action completed">Hoàn
                                thành</button>
                            <button
                                v-if="[convertStatusToText(0), convertStatusToText(1)].includes(convertStatusToText(order.status))"
                                @click="updateStatus(order, 'cancelled')" class="btn-cancel">Hủy đơn</button>
                            <button v-else @click="openDetail(order)" class="btn-detail">Chi tiết</button>
                        </td>
                    </tr>
                </tbody>
            </table>

        </div>
        <div class="navigation" v-if="totalPages">
            <button @click="currentPage = 1" :disabled="currentPage === 1" class="navigation-item previous">
                <i class="bi bi-chevron-double-left"></i>
            </button>
            <button @click="currentPage--" :disabled="currentPage === 1" class="navigation-item previous">
                <i class="bi bi-chevron-left"></i>
            </button>
            <button v-for="page in totalPages.slice(minPageIndex - 1, minPageIndex - 1 + maxPageIndex)" :key="page"
                @click="currentPage = page" class="navigation-item" :class="currentPage == page ? 'active' : ''">
                {{ page }}
            </button>
            <button @click="currentPage++" :disabled="currentPage === totalPages.length" class="navigation-item next">
                <i class="bi bi-chevron-right"></i>
            </button>
            <button @click="currentPage = totalPages.length" :disabled="currentPage === totalPages.length"
                class="navigation-item next">
                <i class="bi bi-chevron-double-right"></i>
            </button>
        </div>

        <!-- Modal chi tiết đơn hàng -->
        <Teleport to="body">
            <div v-if="selectedOrder" class="modal-overlay" @click="selectedOrder = null"></div>
            <div v-if="selectedOrder" class="detail-modal">
                <div class="modal-header">
                    <h3>Chi tiết đơn hàng #{{ selectedOrder.orderId }} <sup class="modal-status" :class="convertStatusToText(selectedOrder.status)">{{ convertStatusToTextVietNamese(selectedOrder.status) }}</sup></h3>
                    <button @click="selectedOrder = null" class="btn btn-close"></button>
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
    font-size: 32px;
    font-weight: 600;
    margin: 0;
    color: #2c3e50;
}


.tabs {
    display: flex;
    gap: 12px;
    margin-bottom: 24px;
    flex-wrap: wrap;
}

.badge {
    margin-left: 8px;
    padding: 2px 8px;
    border-radius: 12px;
    font-size: 12px;
}


.order-container {
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

.user-table th:last-child,
.user-table td:last-child {
    width: 220px;
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

.status-pending {
    --pending-color: #f1c40f;
    background: #f1c40f;
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
}

.status-preparing {
    --praparing-color: #3498db;
    background: #3498db;
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
}

.status-delivering {
    --delivering-color: #e67e22;
    background: #e67e22;
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
}

.status-completed {
    --completed-color: #2ecc71;
    background: #2ecc71;
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
}

.status-cancelled {
    --cancelled-color: #e74c3c;
    background: #e74c3c;
    display: inline-block;
    width: 10px;
    height: 10px;
    border-radius: 50%;
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

.btn-detail,
.btn-action,
.btn-cancel {
    flex: 1;
    margin-right: 4px;
    max-width: max-content;
    padding: 8px 16px;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 12px;
    font-weight: 500 !important;
}

.btn-detail {
    background: var(--info-color);
    color: white;
}

.btn-action.preparing {
    background: var(--praparing-color);
    color: white;
}

.btn-action.delivering {
    background: var(--delivering-color);
    color: white;
}

.btn-action.completed {
    background: var(--completed-color);
    color: white;
}

.btn-cancel {
    background: var(--danger-color);
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
    background: #ffffff1f;
    color: #333;
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-radius: 16px 16px 0 0;
    border-bottom: 2px solid #eee;
}
.modal-status {
    padding: 3px 8px;
    border-radius: 8px;
    font-size: 14px;
    color: #666;
}
.modal-status.pending {
    background-color: var(--pending-color);
    color: white;
}
.modal-status.preparing {
    background-color: var(--praparing-color);
    color: white;
}
.modal-status.delivering {
    background-color: var(--delivering-color);
    color: white;
}
.modal-status.completed {
    background-color: var(--completed-color);
    color: white;
}
.modal-status.cancelled {
    background-color: var(--cancelled-color);
    color: white;
}
.btn-close {
    /* background: none; */
    border: none;
    color: white;
    font-size: 28px;
    cursor: pointer;
}

.modal-body {
    padding: 0 24px 12px 24px;
    overflow-y: auto;
}

.info-row {
    font-size: 16px;
    margin: 12px 0;
}

.items-list h4 {
    /* margin: 0 12px; */
    border-bottom: 1px solid #eee;
    padding-bottom: 8px;
    font-size: 18px;
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
    border-top: 2px solid #eee;
    margin-top: 20px;
    padding-top: 12px;
    font-size: 20px;
    font-weight: bold;
    text-align: right;
    color: var(--primary-color);
}
</style>