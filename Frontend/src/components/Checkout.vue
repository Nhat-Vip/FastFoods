<script setup>
import api from '@/api';
import { useCartStore } from '@/stores/cart';
import useVuelidate from '@vuelidate/core';
import { helpers, required } from '@vuelidate/validators';
import { onUnmounted, onUpdated } from 'vue';
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const router = useRouter();
const cart = useCartStore();

const cartItems = ref([]);

const orderInfo = ref({
    fullName: '',
    phone: '',
    address: '',
    note: '',

})
const phoneValidator = helpers.withMessage(
    'Số điện thoại không hợp lệ',
    value => /^0\d{9}$/.test(value)
);
const rules = {
    fullName: { required: helpers.withMessage('Vui lòng nhập họ và tên', required) },
    phone: {
        required: helpers.withMessage('Vui lòng nhập số điện thoại', required),
        phoneValidator
    },
    address: { required: helpers.withMessage('Vui lòng nhập số địa chỉ', required) },
}

const v$ = useVuelidate(rules, orderInfo);

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
    const isValid = await v$.value.$validate();
    if (!isValid) {
        return;
    }

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
            <h2><span>Đặt hàng</span></h2>

            <!-- Danh sách món đã chọn -->
            <div v-if="cartItems.length === 0" class="empty-cart">
                <p>Chưa có món nào trong giỏ hàng</p>
                <router-link to="/home" class="btn-back-menu">Chọn món ăn ngay</router-link>
            </div>

            <div v-else class="checkout">
                <div class="infomation">
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
                                        <button @click="updateQuantity(item, -1)"
                                            :disabled="item.quantity <= 1">-</button>
                                        <span class="quantity">{{ item.quantity ?? 1 }}</span>
                                        <button @click="updateQuantity(item, 1)">+</button>
                                    </div>
                                </div>
                            </div>
                            <button @click="removeItem(item)" class="btn-remove" title="Xóa món">
                                <i class="bi bi-trash3-fill"></i>
                            </button>

                        </div>
                    </div>

                    <!-- Form thông tin giao hàng -->
                    <div class="delivery-form">
                        <h3>Thông tin nhận hàng</h3>
                        <!-- Thông báo -->
                        <div v-if="notification" class="notification" :class="notification.type">
                            {{ notification.message }}
                        </div>
                        <div class="form-group">
                            <input v-model="orderInfo.fullName" placeholder="Họ và tên *" @blur="v$.fullName.$touch" />
                            <div v-if="v$.fullName.$error" class="text-danger">
                                {{ v$.fullName.$errors[0].$message }}
                            </div>
                        </div>
                        <div class="form-group">
                            <input v-model="orderInfo.phone" placeholder="Số điện thoại *" type="tel"
                                @blur="v$.phone.$touch" />
                            <div v-if="v$.phone.$error" class="text-danger">
                                {{ v$.phone.$errors[0].$message }}
                            </div>
                        </div>
                        <div class="form-group">
                            <input v-model="orderInfo.address" placeholder="Địa chỉ giao hàng chi tiết *"
                                @blur="v$.address.$touch" />
                            <div v-if="v$.address.$error" class="text-danger">
                                {{ v$.address.$errors[0].$message }}
                            </div>
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
                </div>
                     <!-- Tổng tiền -->
                <div class="order-summary">
                    <div class="summary-row">
                        <span>Tạm tính ({{ cartItems.length }} món)</span>
                        <span class="row-price">{{ formatPrice(subTotal) }}₫</span>
                    </div>
                    <div class="summary-row fee">
                        <span>Phí vận chuyển</span>
                        <span class="row-price">{{ formatPrice(deliveryFee) }}₫</span>
                    </div>
                    <div class="summary-row total">
                        <span>Tổng cộng</span>
                        <span class="total-price">{{ formatPrice(total) }}₫</span>
                    </div>
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
    max-width: 1200px;
    margin: 0 auto;
    /* background: white; */
    border-radius: 16px;
    /* box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1); */
}

.order-card h2{
    position: relative;
    text-align: center;
    color: #2c3e50;
    margin-bottom: 30px;
    font-size: 32px;
}

.order-card h2::before{
    content: '';
    position: absolute;
    top:50%;
    left: 0;
    transform: translateY(50%);
    width: 100%;
    height: 2px;
    border-radius: 20px;
    background: #333;
}

.order-card h2 span{
    position: relative;
    background: #f5f5f5;
    padding: 0 20px;
    font-weight: 500;
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

.checkout{
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 40px;
}

.infomation{
    background: white;
    padding: 30px;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
    border-radius: 16px;
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
    justify-content: space-between;
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
    background: inherit;
    border: none;
    font-size: 18px;
    cursor: pointer;
}

.quantity-control button:hover {
    background: #eee;
}

.quantity-control span {
    padding: 0 14px;
    font-weight: bold;
}

.btn-remove {
    background: rgba(227, 8, 8, 0.7);
    border: none;
    padding: 5px 10px;
    border-radius: 5px;
    font-weight: 500;
    color: white;
    font-size: 16px;
    cursor: pointer;
}

.btn-remove:hover {
    background: rgba(227, 8, 8);
}

.order-summary {
    background: white;
    padding: 20px;
    border-radius: 12px;
    height: max-content;
    box-shadow:  0 10px 30px rgba(0, 0, 0, 0.1);
}

.summary-row {
    display: flex;
    justify-content: space-between;
    margin: 12px 0;
    font-size: 16px;
    color: #333333ca;
}
.row-price{
    font-weight: 500;
    color: #333;
}
.total {
    font-weight: bold;
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
    position: relative;
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
    overflow: hidden;
}

/* .btn-place-order:hover:not(:disabled) {
    background: #c0392b;
} */
.btn-place-order::before{
    content: '';
    position: absolute;
    top: 0;
    left: -30px;
    width: 20px;
    height: 100%;
    background: rgba(255, 255, 255, 0.618);
    transform: skewX(-25deg);
    transition: all 0.5s ease;
}
.btn-place-order:hover::before{
    left: 120%;
}

.btn-place-order:disabled {
    background: #ccc;
    cursor: not-allowed;
}

.notification {
    margin-top: 20px;
    margin-bottom: 20px;
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