<script setup>
import api from '@/api';
import { useCartStore } from '@/stores/cart';
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

// Giỏ hàng (giả lập – sau này dùng Pinia hoặc localStorage)
// const cartItems = ref([
//     {
//         id: 1,
//         name: 'Combo Gà Rán + Pepsi',
//         price: 129000,
//         quantity: 2,
//         imageUrl: 'https://images.unsplash.com/photo-1626082927389-7e8bc2b2d1e0?w=400',
//         isCombo: true,
//         comboItems: ['Gà rán', 'Khoai chiên', 'Pepsi']
//     },
//     {
//         id: 2,
//         name: 'Burger Bò Phô Mai',
//         price: 69000,
//         quantity: 1,
//         imageUrl: 'https://images.unsplash.com/photo-1565299507175-7b1d1b6b6b6a?w=400',
//         isCombo: false
//     }
// ])
const route = useRoute();
const router = useRouter();
const cart = useCartStore();

const cartItems = ref([]);

const orderInfo = ref({
    fullName: '',
    phone: '',
    address: '',
    note: '',

})

const deliveryFee = 20000
const isPlacing = ref(false)
const notification = ref(null)

// Tính toán tổng
const subTotal = computed(() => cartItems.value.reduce((sum, item) => sum + (item.price * item.quantity), 0))
const total = computed(() => subTotal.value + deliveryFee)

const formatPrice = (price) => price?.toLocaleString('vi-VN')

// Cập nhật số lượng
const updateQuantity = (item, change) => {
    const newQty = item.quantity + change
    if (newQty < 1) return
    item.quantity = newQty
}

// Xóa món
const removeItem = (item) => {
    cart.removeCart(item.id, item.isCombo, false);
    if (cart.isOrder) {
        cartItems.value = cart.cartOrder;
    }
    else {
        cartItems.value = cart.cartItem;
    }
}

function mapToOrderItem(item) {
    return {
        itemType: item.isCombo ? 1 : 0,

        fastFoodId: item.isCombo ? null : item.id,
        comboId: item.isCombo ? item.id : null,

        quantity: item.quantity,
        unitPrice: item.price,

        fastFoodName: item.isCombo ? null : item.name,
        comboName: item.isCombo ? item.name : null
    };
}


// Đặt hàng
const placeOrder = async () => {
    if (!orderInfo.value.fullName || !orderInfo.value.phone || !orderInfo.value.address) {
        showNotification('Vui lòng điền đầy đủ thông tin!', 'error')
        return
    }
    const orderItems = cartItems.value.map(item => mapToOrderItem(item));

    console.log("OrderItem: ", cartItems.value);

    const orderData = {
        userId: Number(localStorage.getItem("userId")),
        customerName: orderInfo.value.fullName,
        totalAmount: total.value,
        address: orderInfo.value.address,
        phone: orderInfo.value.phone,
        notes: orderInfo.value.note ?? "",
        orderItems: orderItems
    }

    isPlacing.value = true

    // Giả lập gọi API
    const res = await api.post("/orders", orderData);
    console.log("Created Order: ", res.data);

    showNotification('Đặt hàng thành công! Chúng tôi sẽ giao trong 30 phút.', 'success')

    setTimeout(() => {
        cart.resetCart();
        cartItems.value = []
        router.push('/')
    }, 2000)
}

const showNotification = (msg, type = 'success') => {
    notification.value = { message: msg, type }
    setTimeout(() => notification.value = null, 4000)
}
// const type = route.query.type;
// const id = route.query.id;
onMounted(async () => {
    console.log("Cart: ", cart.cartItem);
    if (cart.isOrder) {
        cartItems.value = cart.cartOrder;
    }
    else {
        cartItems.value = cart.cartItem;
    }
});
</script>
<template>
    <div class="order-container">
        <div class="order-card">
            <h2> Order</h2>

            <!-- Danh sách món đã chọn -->
            <div v-if="cartItems.length === 0" class="empty-cart">
                <p>Chưa có món nào trong giỏ hàng</p>
                <router-link to="/home" class="btn-back-menu">Chọn món ăn ngay</router-link>
            </div>

            <div v-else>
                <div class="cart-items">
                    <div v-for="item in cartItems" :key="item.id" class="cart-item">
                        <img :src="item.imageUrl" :alt="item.name" class="item-thumb" />

                        <div class="item-info">
                            <div>
                                <h4 class="item-name">{{ item.name }}</h4>

                                <p v-if="item.isCombo" class="combo-note">
                                    Combo • {{item.comboItems.map(food => food.fastFood.name).join(' • ')}}
                                </p>

                                <span class="price">{{ formatPrice(item.price) }}₫</span>
                            </div>
                            <div class="item-footer">

                                <div class="quantity-control">
                                    <button @click="updateQuantity(item, -1)" :disabled="item.quantity <= 1">-</button>
                                    <span class="quantity">{{ item.quantity ?? 1 }}</span>
                                    <button @click="updateQuantity(item, 1)">+</button>
                                </div>
                            </div>
                        </div>

                        <button @click="removeItem(item)" class="btn-remove" title="Xóa món">
                            Xóa món
                            <i class="fas fa-trash-alt"></i>
                        </button>
                    </div>
                </div>

                <!-- Tổng tiền -->
                <div class="order-summary">
                    <div class="summary-row">
                        <span>Tạm tính ({{ cartItems.length }} món)</span>
                        <strong>{{ formatPrice(subTotal) }}₫</strong>
                    </div>
                    <div class="summary-row fee">
                        <span>Phí vận chuyển</span>
                        <strong>{{ formatPrice(deliveryFee) }}₫</strong>
                    </div>
                    <div class="summary-row total">
                        <span>Tổng cộng</span>
                        <strong class="total-price">{{ formatPrice(total) }}₫</strong>
                    </div>
                </div>

                <!-- Form thông tin giao hàng -->
                <div class="delivery-form">
                    <h3>Thông tin nhận hàng</h3>
                    <div class="form-group">
                        <input v-model="orderInfo.fullName" placeholder="Họ và tên *" required />
                    </div>
                    <div class="form-group">
                        <input v-model="orderInfo.phone" placeholder="Số điện thoại *" type="tel" required />
                    </div>
                    <div class="form-group">
                        <input v-model="orderInfo.address" placeholder="Địa chỉ giao hàng chi tiết *" required />
                    </div>
                    <div class="form-group">
                        <textarea v-model="orderInfo.note" placeholder="Ghi chú cho shipper (không bắt buộc)"
                            rows="2"></textarea>
                    </div>
                </div>

                <!-- Nút đặt hàng -->
                <button @click="placeOrder" class="btn-place-order" :disabled="isPlacing">
                    {{ isPlacing ? 'Đang xử lý...' : 'Đặt hàng ngay' }}
                </button>

                <!-- Thông báo -->
                <div v-if="notification" class="notification" :class="notification.type">
                    {{ notification.message }}
                </div>
            </div>
        </div>
    </div>
</template>
<style scoped>
.order-container {
    padding: 30px 20px;
    background: #f5f5f5;
    min-height: 100vh;
}

.order-card {
    max-width: 800px;
    margin: 0 auto;
    background: white;
    border-radius: 16px;
    padding: 30px;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
}

h2 {
    text-align: center;
    color: #2c3e50;
    margin-bottom: 30px;
    font-size: 26px;
}

.empty-cart {
    text-align: center;
    padding: 60px 20px;
    color: #999;
}

.btn-back-menu {
    display: inline-block;
    margin-top: 20px;
    padding: 12px 30px;
    background: #2874f0;
    color: white;
    text-decoration: none;
    border-radius: 8px;
}

.cart-items {
    border-bottom: 1px solid #eee;
    padding-bottom: 20px;
    margin-bottom: 20px;
}

.cart-item {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 15px;
    padding: 15px 0;
    border-bottom: 1px dashed #eee;
}

.item-thumb {
    width: 100px;
    height: 80px;
    object-fit: cover;
    border-radius: 8px;
}

.item-info {
    width: 70%;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.item-info h4 {
    margin: 0 0 6px;
    font-size: 16px;
}

.combo-note {
    font-size: 13px;
    color: #2874f0;
    margin: 4px 0;
}

.price-quantity {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 10px;
}

.price {
    font-weight: bold;
    color: #e67e22;
}

.quantity-control {
    display: flex;
    align-items: center;
    border: 1px solid #ddd;
    border-radius: 6px;
    overflow: hidden;
}

.quantity-control button {
    width: 34px;
    height: 34px;
    background: #f9f9f9;
    border: none;
    font-size: 18px;
    cursor: pointer;
}

.quantity-control span {
    padding: 0 14px;
    font-weight: bold;
}

.btn-remove {
    background: rgba(227, 8, 8, 0.889);
    border: none;
    padding: 5px 10px;
    border-radius: 5px;
    font-weight: 500;
    color: white;
    font-size: 16px;
    cursor: pointer;
}

.order-summary {
    background: #f8f9fa;
    padding: 20px;
    border-radius: 12px;
    margin: 25px 0;
}

.summary-row {
    display: flex;
    justify-content: space-between;
    margin: 12px 0;
    font-size: 16px;
}

.total {
    font-size: 20px;
    font-weight: bold;
    color: #2c3e50;
    padding-top: 12px;
    border-top: 2px solid #eee;
}

.total-price {
    color: #e74c3c;
    font-size: 24px;
}

.delivery-form h3 {
    margin: 30px 0 20px;
    color: #2c3e50;
}

.form-group {
    margin-bottom: 16px;
}

.form-group input,
.form-group textarea {
    width: 100%;
    padding: 14px;
    border: 1px solid #ddd;
    border-radius: 8px;
    font-size: 15px;
}

.btn-place-order {
    width: 100%;
    padding: 16px;
    background: #e74c3c;
    color: white;
    border: none;
    border-radius: 12px;
    font-size: 18px;
    font-weight: bold;
    cursor: pointer;
    margin-top: 20px;
    transition: all 0.3s;
}

.btn-place-order:hover:not(:disabled) {
    background: #c0392b;
    transform: translateY(-2px);
}

.btn-place-order:disabled {
    background: #ccc;
    cursor: not-allowed;
}

.notification {
    margin-top: 20px;
    padding: 14px;
    border-radius: 8px;
    text-align: center;
    font-weight: 500;
}

.notification.success {
    background: #d4edda;
    color: #155724;
}

.notification.error {
    background: #f8d7da;
    color: #721c24;
}
</style>