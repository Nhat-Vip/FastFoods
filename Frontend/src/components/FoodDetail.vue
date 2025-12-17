<script setup>
import api from '@/api'
import { useCartStore } from '@/stores/cart';
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

// Nhận dữ liệu từ route hoặc props (tùy cách bạn truyền)
const route = useRoute()
const router = useRouter();
const cart = useCartStore();
// Dữ liệu món ăn (sau này lấy từ API)
const food = ref({
    id: 1,
    name: "Combo Gà Rán + Khoai + Pepsi",
    price: 129000,
    originalPrice: 165000, // giá gốc (nếu có khuyến mãi)
    imageUrl: "https://images.unsplash.com/photo-1626082927389-7e8bc2b2d1e0?w=800",
    description: "Combo siêu hời dành cho 1 người, gồm gà rán giòn tan, khoai tây chiên và Pepsi mát lạnh.",
    isCombo: true,
    comboItems: [
        "1 miếng gà rán nguyên vị",
        "1 phần khoai tây chiên lớn",
        "1 ly Pepsi 500ml"
    ]
})

const fetchFood = async () => {
    var type = route.query.type;
    var foodId = route.query.id;
    if (type == "Combo") {
        const res = await api.get(`/combos/${foodId}`);
        food.value.name = res.data.name;
        food.value.imageUrl = res.data.imageUrl;
        food.value.id = res.data.comboId;
        food.value.price = res.data.price;
        food.value.description = res.data.description;
        food.value.isCombo = true;
        food.value.comboItems = res.data.comboItems;
        console.log("Comboitems :", res.data.comboItems);
        
    }
    else {
        const res = await api.get(`/fastfoods/${foodId}`);
        console.log("foodData: ", res.data);
        food.value.name = res.data.name;
        food.value.imageUrl = res.data.imageUrl;
        food.value.id = res.data.fastFoodId;
        food.value.price = res.data.price;
        food.value.description = res.data.description;
        food.value.isCombo = false;
        food.value.comboItems = [];
    }
}



const quantity = ref(1)
const notification = ref(null)

const formatPrice = (price) => {
    return price?.toLocaleString('vi-VN')
}

const increase = () => quantity.value++
const decrease = () => {
    if (quantity.value > 1) quantity.value--
}

const total = computed(() => {
    return quantity.value * food.value.price;
})

const addToCart = () => {
    food.value.quantity = quantity.value;
    cart.addCart(food.value,true);
    notification.value = {
        type: 'success',
        message: `Đã thêm ${quantity.value} × "${food.value.name}" vào giỏ hàng!`
    }

    setTimeout(() => {
        notification.value = null
    }, 3000)
}
const handleOrder = () => {
    food.value.quantity = quantity.value;
    cart.addCart(food.value, false);
    router.push("/checkout");

}

onMounted(async () => {
    await fetchFood();
})
// Nếu là món lẻ thì comboItems = null, isCombo = false
// Ví dụ món lẻ:
// isCombo: false,
// comboItems: null,
</script>
<template>
    <div class="food-detail-container">
        <div class="food-detail-card">
            <!-- Hình ảnh lớn -->
            <div class="image-section">
                <img :src="food.imageUrl" :alt="food.name" class="main-image" />
                <div v-if="food.isCombo" class="combo-badge">COMBO SIÊU TIẾT KIỆM</div>
            </div>

            <!-- Thông tin chi tiết -->
            <div class="info-section">
                <h1 class="food-name">{{ food.name }}</h1>

                <!-- Badge Combo nếu có -->
                <!-- <div v-if="food.isCombo" class="tag-combo">Combo</div>
                <div v-if="!food.isCombo" class="tag-fastFood">FastFood</div> -->

                <!-- Giá -->
                <div class="price-section">
                    <span class="price">{{ formatPrice(food.price) }}₫</span>
                    <span v-if="food.originalPrice" class="old-price">
                        {{ formatPrice(food.price + 20000) }}₫
                    </span>
                </div>

                <!-- Mô tả ngắn (tùy chọn) -->
                <p class="description" v-if="food.description">
                    {{ food.description }}
                </p>

                <!-- Nếu là Combo → hiện danh sách món -->
                <div v-if="food.isCombo && food.comboItems?.length" class="combo-list">
                    <h3>Combo gồm có:</h3>
                    <ul>
                        <li v-for="(item, index) in food.comboItems" :key="index">
                            <span>{{ item.fastFood?.name }}</span>
                        </li>
                    </ul>
                </div>

                <!-- Nút hành động -->
                <div class="action-buttons">
                    <div class="quantity-control">
                        <button @click="decrease" :disabled="quantity <= 1">-</button>
                        <span class="quantity">{{ quantity }}</span>
                        <button @click="increase">+</button>
                    </div>
                    <div class="total">
                        <span>Tổng: {{formatPrice(total)}}₫</span>
                    </div>
                </div>
                <div class="action-buttons">
                    <button class="btn-order" @click="handleOrder">
                        Đặt hàng
                    </button>
                    <button class="btn-add-to-cart" @click="addToCart">
                        <i class="bi bi-cart"></i>
                    </button>
                </div>

                <!-- Thông báo -->
                <div v-if="notification" class="notification" :class="notification.type">
                    {{ notification.message }}
                </div>
            </div>
        </div>
    </div>
</template>
<style scoped>
.food-detail-container {
    padding: 30px 20px;
    background: #f8f9fa;
    min-height: 100vh;
}

.food-detail-card {
    max-width: 1200px;
    margin: 0 auto;
    background: white;
    border-radius: 16px;
    overflow: hidden;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 40px;
}

.image-section {
    position: relative;
    overflow: hidden;
    padding: 5px 10px;
}

.main-image {
    width: 100%;
    height: 100%;
    object-fit: cover;
    min-height: 400px;
}

.combo-badge {
    position: absolute;
    top: 20px;
    left: 20px;
    background: #e74c3c;
    color: white;
    padding: 8px 16px;
    border-radius: 30px;
    font-weight: bold;
    font-size: 14px;
    box-shadow: 0 4px 15px rgba(231, 76, 60, 0.4);
}

.info-section {
    padding: 30px;
    display: flex;
    flex-direction: column;
    justify-content: center;
}

.food-name {
    font-size: 28px;
    margin: 0 0 12px 0;
    color: #333;
    font-weight: 600;
}
/* 
.tag-combo, .tag-fastFood {
    display: inline-block;
    background: #2874f0;
    color: white;
    padding: 4px 12px;
    border-radius: 20px;
    font-size: 12px;
    font-weight: bold;
    align-self: flex-start;
    margin-bottom: 16px;
} */

.price-section {
    margin: 0;
}

.price {
    font-size: 32px;
    font-weight: bold;
    color: #e67e22;
}

.old-price {
    margin-left: 12px;
    color: #999;
    text-decoration: line-through;
    font-size: 18px;
}

.description {
    color: #555;
    line-height: 1.6;
    margin: 10px 0;
    font-size: 15px;
}

.combo-list h3 {
    margin: 24px 0 12px;
    color: #2c3e50;
    font-size: 18px;
}

.combo-list ul {
    list-style: none;
    padding: 0;
}

.combo-list li {
    padding: 8px 0;
    color: #444;
    font-size: 15px;
}

.combo-list li::before {
    content: "✓ ";
    color: #27ae60;
    font-weight: bold;
    margin-right: 8px;
}

.action-buttons {
    display: flex;
    align-items: center;
    gap: 20px;
    margin-top: 10px;
}

.quantity-control {
    display: flex;
    align-items: center;
    border: 2px solid #ddd;
    border-radius: 8px;
    overflow: hidden;
}

.quantity-control button {
    width: 40px;
    height: 40px;
    background: #f8f9fa;
    border: none;
    font-size: 20px;
    cursor: pointer;
}

.quantity-control button:hover {
    background: #eee;
}

.quantity {
    padding: 0 20px;
    font-size: 18px;
    font-weight: bold;
}

.total{
    width: 90%;
    text-align: end;
}
.total span{
    font-size: 22px;
    font-weight: bold;
    color: #e67e22;
}

.btn-add-to-cart {
    width: 50px;
    background: #2874f0;
    color: white;
    border: none;
    max-height: max-content !important;
    padding: 10px 18px;
    border-radius: 8px;
    font-size: 16px;
    font-weight: bold;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
    transition: all 0.2s;
}

.btn-add-to-cart:hover {
    background: #1160d1;
    transform: translateY(-2px);
}

.btn-order{
    background-color: rgba(240, 139, 15, 0.837);
    color: white;
    border: none;
    flex: 1;
    text-align: center;
    padding: 10px 18px;
    border-radius: 10px;
    font-size: 18px;
    font-weight: bold;
    transition: all 0.2s;
}
.btn-order:hover{
    background-color: rgba(240, 139, 15);
    transform: translateY(-2px);
}
.notification {
    margin-top: 20px;
    padding: 12px;
    border-radius: 8px;
    text-align: center;
    font-weight: 500;
}

.notification.success {
    background: #d4edda;
    color: #155724;
    border: 1px solid #c3e6cb;
}

/* Responsive */
@media (max-width: 768px) {
    .food-detail-card {
        grid-template-columns: 1fr;
    }

    .main-image {
        min-height: 300px;
    }
}
</style>