<script setup>
import { required } from '@vuelidate/validators';

defineProps({
    foodId: {
        Number,
        required: true
    },
    imgUrl: {
        type: String,
        default: 'https://via.placeholder.com/300x200?text=No+Image'
    },
    name: {
        type: String,
        required: true
    },
    price: {
        type: Number,
        required: true
    },
    comboItem: {
        type: Array,
        default: () => []
    },
    // Thêm props tùy chọn
    isCombo: {
        type: Boolean,
        default: false
    }
})
</script>

<template>
    <div class="fastfood-card">
        <router-link class="text-decoration-none" :to="{
            name: 'FoodDetail',
            query: { type: isCombo ? 'Combo' : 'Food', id: foodId }
        }">
            <!-- Hình ảnh -->
            <div class="card-image">
                <img :src="imgUrl" :alt="name" />
                <!-- Badge Combo nếu là combo -->
                <div v-if="isCombo" class="badge-combo">COMBO</div>
            </div>

            <!-- Nội dung -->
            <div class="card-content">
                <h3 class="food-name">{{ name }}</h3>

                <!-- Nếu là combo → hiện các món con -->
                <div v-if="comboItem && comboItem.length > 0" class="combo-items">
                    <p v-for="(item, index) in comboItem" :key="index" class="combo-item">
                        {{ item }}
                    </p>
                </div>

                <!-- Giá -->
                <div class="price-actions">
                    <span class="price">{{ price.toLocaleString('vi-VN') }}₫</span>
                </div>
            </div>
        </router-link>
    </div>
</template>

<style scoped>
.fastfood-card {
    background: white;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.08);
    transition: all 0.3s ease;
    width: 100%;
    max-width: 300px;
}

.fastfood-card:hover {
    transform: translateY(-8px);
    box-shadow: 0 12px 25px rgba(0, 0, 0, 0.15);
}

.card-image {
    position: relative;
    height: 180px;
    overflow: hidden;
}

.card-image img {
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.4s ease;
}

.fastfood-card:hover .card-image img {
    transform: scale(1.08);
}

.badge-combo {
    position: absolute;
    top: 10px;
    left: 10px;
    background: #e74c3c;
    color: white;
    font-size: 12px;
    font-weight: bold;
    padding: 4px 10px;
    border-radius: 20px;
    letter-spacing: 0.5px;
}

.card-content {
    padding: 16px;
}

.food-name {
    margin: 0 0 10px 0;
    font-size: 18px;
    font-weight: 600;
    color: #2c3e50;
    line-height: 1.3;
}

.combo-items {
    margin: 8px 0 12px;
    padding-left: 4px;
}

.combo-item {
    font-size: 13px;
    color: #666;
    margin: 4px 0;
    line-height: 1.4;
}

.combo-item::before {
    content: "• ";
    color: #2874f0;
    font-weight: bold;
}

.price-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 12px;
}

.price {
    font-size: 20px;
    font-weight: bold;
    color: #e67e22;
}

.btn-add {
    width: 40px;
    height: 40px;
    background: #2874f0;
    color: white;
    border: none;
    border-radius: 50%;
    font-size: 18px;
    cursor: pointer;
    transition: all 0.2s;
    display: flex;
    align-items: center;
    justify-content: center;
}

.btn-add:hover {
    background: #1160d1;
    transform: scale(1.1);
}

/* Responsive */
@media (max-width: 480px) {
    .fastfood-card {
        max-width: 100%;
    }
}
</style>