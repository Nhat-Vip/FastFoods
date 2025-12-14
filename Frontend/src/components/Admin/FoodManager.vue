<script setup>
import api from '@/api'
import { AlertError, AlertSuccess } from '@/Notification'
import { ref, onMounted } from 'vue'

// Giả lập dữ liệu (sau này lấy từ API)
const fastFoods = ref([])

const categories = ref([
    { id: 1, name: "Burger", color: "#e74c3c" },
    { id: 2, name: "Gà rán", color: "#e74c3c" },
    { id: 3, name: "Pizza", color: "#e74c3c" },
    { id: 4, name: "Nước uống", color: "#e74c3c" }
])

// Modal & form
const isModalOpen = ref(false)
const isEditMode = ref(false)
const currentFood = ref(null)
const foodToDelete = ref(null)
const showConfirm = ref(false)
const previewUrl = ref('')
const selectedFile = ref(null);
const isLockFood = ref(false);

const form = ref({
    name: '',
    description: '',
    price: 0,
    categoryId: null,
    imageFile: null
})

const openCreateModal = () => {
    isEditMode.value = false
    form.value = { name: '', description: '', price: 0, categoryId: null }
    previewUrl.value = ''
    isModalOpen.value = true
}

const openEditModal = (food) => {
    isEditMode.value = true
    currentFood.value = food
    form.value = {
        name: food.name,
        description: food.description,
        price: food.price,
        categoryId: food.categoryId
    }
    previewUrl.value = food.imageUrl || ''
    isModalOpen.value = true
}

const closeModal = () => {
    isModalOpen.value = false
    currentFood.value = null
}

const onFileChange = (e) => {
    const file = e.target.files[0]
    if (file) {
        selectedFile.value = file;
        previewUrl.value = URL.createObjectURL(file)
    }
}

const saveFood = async () => {
    try {

        if (isEditMode.value) {
            if (selectedFile.value) {
                form.value.imageFile = selectedFile.value;
            }
            console.log("Updating Food: ", form.value);
            const res = await api.put(`/fastfoods/${currentFood.value.fastFoodId}`, form.value, {
                headers: {
                    "Content-Type": "multipart/form-data"
                }
            });

            Object.assign(currentFood.value, { ...res.data })
            AlertSuccess('Cập nhật món thành công!')
            form.value = [];
            selectedFile.value = null;
        } else {
            if (!selectedFile.value) {
                AlertError("Vui lòng chọn ảnh cho sản phẩm");
                return;
            }
            // if (!form.value.categoryId) {
            //     AlertError("Vui lòng chọn danh mục của sản phẩm");
            //     return;
            // }
            form.value.imageFile = selectedFile.value;
            const res = await api.post("/fastfoods", form.value, {
                headers: {
                    "Content-Type": "multipart/form-data"
                }
            });
            fastFoods.value.push(res.data);
            form.value = [];
            selectedFile.value = null;
            AlertSuccess('Thêm món thành công!')
        }
        closeModal()
    }
    catch (e) {
        console.log("Có lỗi xảy ra", e);
        AlertError("Có lỗi xảy ra vui lòng thử lại");
    }
}

const toggleActive = (food) => {
    foodToDelete.value = food;
    isLockFood.value = true;
    showConfirm.value = true;
}

const lockFood = async () => {
    try {
        await api.put(`/fastfoods/lock/${foodToDelete.value.fastFoodId}`);
        foodToDelete.value.isActive = !foodToDelete.value.isActive;
        AlertSuccess(`${foodToDelete.value.isActive ? "Bán lại" : "Ngưng bán"} ${foodToDelete.value.name} thành công`);
        showConfirm.value = false;
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            console.log("Có lỗi xảy ra", e);
            AlertError("Có lỗi xảy ra vui lòng thử lại");
        }
    }
}

const confirmDelete = (food) => {
    isLockFood.value = false;
    foodToDelete.value = food
    showConfirm.value = true
}

const deleteFood = async () => {
    try { 

        await api.delete(`/fastfoods/${foodToDelete.value.fastFoodId}`);
        fastFoods.value = fastFoods.value.filter(f => f.fastFoodId !== foodToDelete.value.fastFoodId)
        AlertSuccess('Đã xóa món!')
        showConfirm.value = false
        foodToDelete.value = null
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            console.log("Có lỗi xảy ra", e);
            AlertError("Có lỗi xảy ra vui lòng thử lại");
        }
    }
}

// Helper
const formatPrice = (p) => p.toLocaleString('vi-VN')
const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN')
const truncate = (text, length) => text.length > length ? text.slice(0, length) + '...' : text
const getCategoryName = (id) => categories.value.find(c => c.categoryId === id)?.categoryName || 'Chưa chọn'
const getCategoryColor = (id) => categories.value.find(c => c.id === id)?.color || '#95a5a6'

onMounted(async() => {
    const resFoods = await api.get("/fastfoods/admin");
    fastFoods.value = resFoods.data;
    const resCategory = await api.get("/categories");
    categories.value = resCategory.data;
})
</script>
<template>
    <div class="fastfood-manager">
        <div class="header">
            <h2>Quản lý món ăn</h2>
            <button @click="openCreateModal" class="btn-add">
                Thêm món mới
            </button>
        </div>

        <!-- Bảng danh sách món ăn -->
        <div class="table-container">
            <table class="food-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Ảnh</th>
                        <th>Tên món</th>
                        <th>Danh mục</th>
                        <th>Giá</th>
                        <th>Trạng thái</th>
                        <th>Ngày tạo</th>
                        <th>Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="food in fastFoods" :key="food.fastFoodId">
                        <td>#{{ food.fastFoodId }}</td>
                        <td>
                            <img :src="food.imageUrl || 'https://via.placeholder.com/80x60?text=Food'" alt="food"
                                class="thumb" />
                        </td>
                        <td class="name-col">
                            <strong>{{ food.name }}</strong>
                            <p class="desc-preview">{{ truncate(food.description, 40) }}</p>
                        </td>
                        <td>
                            <span class="category-badge">
                                {{ getCategoryName(food.categoryId) || 'Chưa chọn' }}
                            </span>
                        </td>
                        <td class="price-col">{{ formatPrice(food.price) }}₫</td>
                        <td>
                            <span class="status-badge" :class="food.isActive ? 'active' : 'inactive'">
                                {{ food.isActive ? 'Đang bán' : 'Ngưng bán' }}
                            </span>
                        </td>
                        <td>{{ formatDate(food.createdAt) }}</td>
                        <td class="actions">
                            <button @click="openEditModal(food)" class="btn-edit">Sửa</button>
                            <button @click="toggleActive(food)" :class="food.isActive ? 'btn-hide' : 'btn-show'">
                                {{ food.isActive ? 'Ngưng bán' : 'Bán lại' }}
                            </button>
                            <button @click="confirmDelete(food)" class="btn-delete">Xóa</button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal Thêm / Sửa món -->
        <Teleport to="body">
            <div v-if="isModalOpen" class="modal-overlay" @click="closeModal"></div>
            <div v-if="isModalOpen" class="food-modal">
                <div class="modal-header">
                    <h3>{{ isEditMode ? 'Sửa món ăn' : 'Thêm món ăn mới' }}</h3>
                    <button @click="closeModal" class="btn-close">X</button>
                </div>

                <form @submit.prevent="saveFood" class="modal-form">
                    <div class="form-row">
                        <div class="form-group">
                            <label class="form-label">Tên món *</label>
                            <input class="form-control" v-model="form.name" required />
                        </div>
                        <div class="form-group">
                            <label class="form-label">Giá (₫) *</label>
                            <input class="form-control" v-model.number="form.price" type="number" min="0" required />
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Danh mục *</label>
                        <select class="form-select" v-model="form.categoryId" required>
                            <option value="">Chưa chọn</option>
                            <option v-for="cat in categories" :value="cat.categoryId">{{ cat.categoryName }}</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Mô tả chi tiết *</label>
                        <textarea v-model="form.description" rows="4" required></textarea>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Ảnh món ăn</label>
                        <input type="file" @change="onFileChange" accept="image/*" class="mt-2 form-control" />
                        <img v-if="previewUrl" :src="previewUrl" class="preview-img" alt="Preview" />
                    </div>

                    <div class="modal-actions">
                        <button type="submit" class="btn-save">
                            {{ isEditMode ? 'Cập nhật' : 'Thêm món' }}
                        </button>
                        <button type="button" @click="closeModal" class="btn-cancel">Hủy</button>
                    </div>
                </form>
            </div>
        </Teleport>

        <!-- Confirm Xóa -->
        <Teleport to="body">
            <div v-if="showConfirm" class="modal-overlay" @click="showConfirm = false"></div>
            <div v-if="showConfirm" class="confirm-modal">
                <div v-if="!isLockFood">
                    <h3>Xóa món ăn?</h3>
                    <p>Bạn có chắc chắn xóa món <strong>{{ foodToDelete?.name }}</strong>?</p>
                    <div class="confirm-actions">
                        <button @click="deleteFood" class="btn-danger">
                            Xóa vĩnh viễn
                        </button>
                        <button @click="showConfirm = false" class="btn-cancel">Hủy</button>
                    </div>
                </div>
                <div v-else>
                    <h3>{{foodToDelete.isActive ? "Ngưng bán" : "Bán lại"}} món ăn</h3>
                    <p>Bạn có chắc chắn {{ foodToDelete.isActive ? "Ngưng bán" : "Bán lại" }} <strong>{{
                            foodToDelete?.name }}</strong>?</p>
                    <div class="confirm-actions">
                        <button @click="lockFood" class="btn-danger">
                            {{ foodToDelete.isActive ? "Ngưng bán" : "Bán lại" }}
                        </button>
                        <button @click="showConfirm = false" class="btn-cancel">Hủy</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>
<style scoped>
.fastfood-manager {
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

.btn-add {
    background: #27ae60;
    color: white;
    padding: 12px 24px;
    border: none;
    border-radius: 8px;
    font-weight: bold;
    cursor: pointer;
}

.table-container {
    background: white;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
    max-height: 600px;
    overflow-y: auto;
}

.food-table {
    width: 100%;
    border-collapse: collapse;
}

.food-table th {
    background: #2c3e50;
    color: white;
    padding: 16px 12px;
    text-align: left;
    font-weight: 600;
}

.food-table td {
    padding: 14px 12px;
    border-bottom: 1px solid #eee;
    vertical-align: middle;
}

.food-table tr:hover {
    background: #f8f9fa;
}

.thumb {
    width: 80px;
    height: 60px;
    object-fit: cover;
    border-radius: 8px;
}

.name-col strong {
    display: block;
    font-size: 15px;
}

.desc-preview {
    margin: 4px 0 0;
    font-size: 12px;
    color: #777;
}

.category-badge {
    background: #e74c3c;
    padding: 4px 12px;
    border-radius: 20px;
    color: white;
    font-size: 12px;
    font-weight: bold;
}

.price-col {
    font-weight: bold;
    color: #e67e22;
    font-size: 16px;
}

.status-badge {
    padding: 6px 12px;
    border-radius: 20px;
    font-size: 12px;
    font-weight: bold;
}

.status-badge.active {
    background: #d4edda;
    color: #155724;
}

.status-badge.inactive {
    background: #f8d7da;
    color: #721c24;
}

.actions button {
    margin: 0 4px;
    padding: 6px 10px;
    border: none;
    border-radius: 6px;
    font-size: 12px;
    cursor: pointer;
}

.btn-edit {
    background: #3498db;
    color: white;
}

.btn-show {
    background: #27ae60;
    color: white;
}

.btn-hide {
    background: #95a5a6;
    color: white;
}

.btn-delete {
    background: #e74c3c;
    color: white;
}

/* Modal styles giống ComboManager */
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.6);
    z-index: 999;
}

.food-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 90%;
    max-width: 650px;
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
    padding: 16px 24px;
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

.modal-form {
    padding: 24px;
    overflow-y: auto;
    max-height: 70vh;
}

.form-row {
    display: flex;
    gap: 16px;
}

.form-row .form-group {
    flex: 1;
}

.form-group {
    margin-bottom: 16px;
}

.form-group label {
    display: block;
    margin-bottom: 8px;
    font-weight: 600;
}

.form-group input,
.form-group textarea,
.form-group select {
    width: 100%;
    border: 1px solid #ddd;
    border-radius: 8px;
}

.preview-img {
    width: 100%;
    max-height: 200px;
    object-fit: cover;
    margin-top: 10px;
    border-radius: 8px;
}

.modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 24px;
}

.btn-save {
    flex: 1;
    background: #27ae60;
    color: white;
    padding: 14px;
    border: none;
    border-radius: 12px;
    font-weight: bold;
}

.btn-cancel {
    flex: 1;
    background: #eee;
    padding: 14px;
    border: none;
    border-radius: 12px;
}
</style>