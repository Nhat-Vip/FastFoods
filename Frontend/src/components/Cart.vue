<script setup>
import { useCartStore } from '@/stores/cart';
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

// Trạng thái mở/đóng cart
const isOpen = ref(false);
const cart = useCartStore();

// Dữ liệu giỏ hàng (sau này dùng Pinia hoặc localStorage)
const cartItems = ref(cart.cartItem);

const deliveryFee = 20000
const router = useRouter();

// Tính toán
const totalItems = computed(() => cartItems.value.reduce((sum, i) => sum + i.quantity, 0))
const subTotal = computed(() => cartItems.value.reduce((sum, i) => sum + i.price * i.quantity, 0))
const total = computed(() => subTotal.value + deliveryFee)

const formatPrice = (price) => price.toLocaleString('vi-VN')

// Hành động
const updateQuantity = (item, change) => {
    const newQty = item.quantity + change
    if (newQty < 1) return
    item.quantity = newQty
}

const removeItem = (item) => {
    cart.removeCart(item.id, item.isCombo, true);
    cartItems.value = cart.cartItem;
}

const handleOrder = () => {
    cart.OrderWith(true);
    router.push("/checkout");
}

</script>
<template>
    <!-- Floating Cart Button (góc phải dưới) -->
    <div class="cart-floating-btn" @click="isOpen = true" v-if="!isOpen && totalItems > 0">
        <i class="bi bi-cart"></i>
        <span class="cart-badge">{{ totalItems }}</span>
        <span class="cart-total">{{ formatPrice(total) }}₫</span>
    </div>

    <!-- Cart Drawer (trượt từ phải sang) -->
    <Teleport to="body">
        <div v-if="isOpen" class="cart-overlay" @click="isOpen = false"></div>

        <div class="cart-drawer" :class="{ open: isOpen }">
            <div class="cart-header">
                <h3 class="text-center"><i class="bi bi-cart"></i> Giỏ hàng</h3>
                <button @click="isOpen = false" class="btn-close">
                    <i class="fas fa-times"></i>
                </button>
            </div>

            <!-- Empty state -->
            <div v-if="cartItems.length === 0" class="cart-empty">
                <i class="fas fa-shopping-bag fa-3x"></i>
                <p>Chưa có món nào trong giỏ</p>
                <router-link to="/menu" class="btn-go-menu" @click="isOpen = false">
                    Chọn món ngay
                </router-link>
            </div>

            <!-- Cart items -->
            <div v-else class="cart-items">
                <div v-for="item in cartItems" :key="item.id" class="cart-item">
                    <img :src="item.imageUrl" :alt="item.name" class="item-thumb" />

                    <div class="item-details">
                        <h4 class="item-name">{{ item.name }}</h4>
                        <p v-if="item.isCombo" class="combo-note">
                            {{ item.comboItems.map(food=>food.fastFood.name)?.join(' • ') }}
                        </p>
                        <div class="item-price">{{ formatPrice(item.price) }}₫</div>
                    </div>

                    <div class="item-actions">
                        <div class="quantity-control">
                            <button @click="updateQuantity(item, -1)" :disabled="item.quantity <= 1">−</button>
                            <span>{{ item.quantity }}</span>
                            <button @click="updateQuantity(item, 1)">+</button>
                        </div>
                        <button @click="removeItem(item)" class="btn-delete">
                            <i class="bi bi-trash-fill"></i>
                        </button>
                    </div>
                </div>
            </div>

            <!-- Tổng tiền + Nút đặt hàng -->
            <div class="cart-footer" v-if="cartItems.length > 0">
                <div class="summary">
                    <div class="row">
                        <span>Tạm tính</span>
                        <strong>{{ formatPrice(subTotal) }}₫</strong>
                    </div>
                    <div class="row">
                        <span>Phí giao hàng</span>
                        <strong>{{ formatPrice(deliveryFee) }}₫</strong>
                    </div>
                    <div class="row total">
                        <span>Tổng cộng</span>
                        <strong class="total-price">{{ formatPrice(total) }}₫</strong>
                    </div>
                </div>

                <button class="btn-checkout" @click="handleOrder(),isOpen = false">
                    Đặt hàng ngay
                </button>
            </div>
        </div>
    </Teleport>
</template>
<style scoped>
/* Floating Button */
.cart-floating-btn {
    position: fixed;
    bottom: 20px;
    right: 20px;
    background: linear-gradient(135deg, #e74c3c, #c0392b);
    color: white;
    padding: 16px 20px;
    border-radius: 50px;
    box-shadow: 0 8px 25px rgba(231, 76, 60, 0.4);
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 12px;
    font-weight: bold;
    z-index: 999;
    transition: all 0.3s ease;
}

.cart-floating-btn:hover {
    transform: scale(1.05);
    box-shadow: 0 12px 30px rgba(231, 76, 60, 0.5);
}

.cart-badge {
    background: white;
    color: #e74c3c;
    min-width: 24px;
    height: 24px;
    border-radius: 50%;
    font-size: 13px;
    font-weight: bold;
    display: flex;
    align-items: center;
    justify-content: center;
}

/* Overlay */
.cart-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.5);
    z-index: 1000;
    backdrop-filter: blur(5px);
}

/* Drawer */
.cart-drawer {
    position: fixed;
    top: 0;
    right: -520px;
    width: 520px;
    height: 100vh;
    background: white;
    box-shadow: -10px 0 30px rgba(0, 0, 0, 0.15);
    z-index: 1001;
    transition: right 0.4s cubic-bezier(0.25, 0.8, 0.25, 1);
    display: flex;
    flex-direction: column;
}

.cart-drawer.open {
    right: 0;
}

.cart-header {
    padding: 20px;
    background: #2874f0;
    color: white;
    display: flex;
    justify-content: center;
    align-items: center;
}

.cart-header h3 {
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

.cart-empty {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: #999;
    text-align: center;
    padding: 40px 20px;
}

.btn-go-menu {
    margin-top: 20px;
    padding: 12px 30px;
    background: #2874f0;
    color: white;
    text-decoration: none;
    border-radius: 30px;
    font-weight: bold;
}

.cart-items {
    flex: 1;
    overflow-y: auto;
    padding: 10px 0;
}

.cart-item {
    display: flex;
    padding: 16px 20px;
    border-bottom: 1px solid #eee;
    align-items: center;
    gap: 14px;
}

.item-thumb {
    width: 70px;
    height: 70px;
    object-fit: cover;
    border-radius: 12px;
}

.item-details {
    flex: 1;
}

.item-name {
    margin: 0 0 6px;
    font-size: 16px;
    font-weight: 600;
    line-height: 1.3;
}

.combo-note {
    font-size: 12.5px;
    color: #2874f0;
    margin: 4px 0;
}

.item-price {
    font-weight: bold;
    color: #e67e22;
    margin-top: 6px;
}

.item-actions {
    display: flex;
    align-items: center;
    gap: 10px;
}

.quantity-control {
    display: flex;
    align-items: center;
    background: #f8f9fa;
    border-radius: 20px;
    overflow: hidden;
    border: 1px solid #ddd;
}

.quantity-control button {
    width: 32px;
    height: 32px;
    background: white;
    border: none;
    font-weight: bold;
    cursor: pointer;
}

.quantity-control span {
    min-width: 36px;
    text-align: center;
    font-weight: bold;
}

.btn-delete {
    background: #fee2e2;
    color: #e74c3c;
    border: none;
    width: 36px;
    height: 36px;
    border-radius: 50%;
    cursor: pointer;
    font-size: 14px;
}

.cart-footer {
    padding: 20px;
    background: #f8f9fa;
    border-top: 1px solid #eee;
}

.summary {
    margin-bottom: 20px;
}

.row {
    display: flex;
    justify-content: space-between;
    margin: 10px 0;
    font-size: 15px;
}

.total {
    font-size: 18px;
    font-weight: bold;
    padding-top: 12px;
    border-top: 2px solid #ddd;
}

.total-price {
    color: #e74c3c;
    font-size: 22px;
}

.btn-checkout {
    display: block;
    width: 100%;
    border: none;
    padding: 16px;
    background: #e74c3c;
    color: white;
    text-align: center;
    border-radius: 12px;
    font-size: 18px;
    font-weight: bold;
    text-decoration: none;
    transition: all 0.3s;
}

.btn-checkout:hover {
    background: #c0392b;
    transform: translateY(-2px);
}

/* Mobile */
@media (max-width: 480px) {
    .cart-drawer {
        width: 100%;
        right: -100%;
    }

    .cart-floating-btn {
        bottom: 15px;
        right: 15px;
        padding: 14px 18px;
    }
}
</style>