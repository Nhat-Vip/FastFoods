<script setup>
import api from '@/api'
import { AlertError, AlertSuccess } from '@/Notification'
import { ref, onMounted, computed } from 'vue'

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
const isDetailMode = ref(false);
const currentFood = ref(null)
const foodToDelete = ref(null)
const showConfirm = ref(false)
const previewUrl = ref('')
const selectedFile = ref(null);
const isLockFood = ref(false);

const currentPage = ref(1);
const itemsPerPage = ref(6);
const totalPages = computed(() => {
    return Array.from({ length: Math.ceil(fastFoods.value.length / itemsPerPage.value) }, (_, i) => i + 1);
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

const form = ref({
    name: '',
    description: '',
    price: 0,
    categoryId: null,
    imageFile: null
})

const openCreateModal = () => {
    isEditMode.value = false
    isDetailMode.value = false
    form.value = { name: '', description: '', price: 0, categoryId: null }
    previewUrl.value = ''
    isModalOpen.value = true
}

const openEditModal = (food) => {
    isEditMode.value = true
    isDetailMode.value = false
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
const openDetailModal = (event, food) => {
    if (event.target.closest(".dropdown")) return;
    isDetailMode.value = true;
    isEditMode.value = false;
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

onMounted(async () => {
    const resFoods = await api.get("/fastfoods/admin");
    fastFoods.value = resFoods.data;
    const resCategory = await api.get("/categories");
    categories.value = resCategory.data;

    console.log("totalPages: ", totalPages.value);
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
                        <th>#</th>
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
                    <tr v-for="food in fastFoods.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage)"
                        @click="openDetailModal($event, food)" :key="food.fastFoodId">
                        <td>{{ food.fastFoodId }}</td>
                        <td>
                            <img :src="food.imageUrl || 'https://via.placeholder.com/80x60?text=Food'" alt="food"
                                class="thumb" />
                        </td>
                        <td class="name-col">
                            <span>{{ food.name }}</span>
                        </td>
                        <td>
                            <span class="category-badge">
                                {{ getCategoryName(food.categoryId) || 'Chưa chọn' }}
                            </span>
                        </td>
                        <td class="price-col">{{ formatPrice(food.price) }}₫</td>
                        <td class="status">
                            <span :class="food.isActive ? 'active' : 'inactive'"></span>
                            <span class="status-badge">
                                {{ food.isActive ? 'Đang bán' : 'Ngưng bán' }}
                            </span>
                        </td>
                        <td>{{ formatDate(food.createdAt) }}</td>
                        <td class="actions dropdown">
                            <button type="button" data-bs-toggle="dropdown">
                                <i class="bi bi-three-dots-vertical"></i>
                            </button>
                            <ul class="dropdown-menu">
                                <li><button @click="openEditModal(food)" class="dropdown-item">Sửa</button></li>
                                <li><button @click="toggleActive(food)" class="dropdown-item">
                                        {{ food.isActive ? 'Ngưng bán' : 'Bán lại' }}
                                    </button></li>
                                <li><button @click="confirmDelete(food)" class="dropdown-item text-danger">Xóa</button>
                                </li>
                            </ul>
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
        <!-- Modal Thêm / Sửa món -->
        <Teleport to="body">
            <div v-if="isModalOpen" class="modal-overlay" @click="closeModal"></div>
            <div v-if="isModalOpen" class="food-modal">
                <div class="modal-header">
                    <h3>{{ isEditMode ? 'Sửa món ăn' : isDetailMode ? 'Chi tiết món ăn' : 'Thêm món ăn mới' }}</h3>
                    <button @click="closeModal" class="btn btn-close"></button>
                </div>

                <form @submit.prevent="saveFood" class="modal-form">
                    <div class="form-row">
                        <div class="form-group">
                            <label class="form-label">Tên món *</label>
                            <input class="form-control" v-model="form.name" required :disabled="isDetailMode" />
                        </div>
                        <div class="form-group">
                            <label class="form-label">Giá (₫) *</label>
                            <input class="form-control" v-model.number="form.price" type="number" min="0" required
                                :disabled="isDetailMode" />
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Danh mục *</label>
                        <select class="form-select" v-model="form.categoryId" required :disabled="isDetailMode">
                            <option value="">Chưa chọn</option>
                            <option v-for="cat in categories" :value="cat.categoryId">{{ cat.categoryName }}</option>
                        </select>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Mô tả chi tiết *</label>
                        <textarea v-model="form.description" class="p-2" rows="4" required
                            :disabled="isDetailMode"></textarea>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Ảnh món ăn</label>
                        <input type="file" @change="onFileChange" accept="image/*" class="mt-2 form-control"
                            :disabled="isDetailMode" />
                        <img v-if="previewUrl" :src="previewUrl" class="preview-img" alt="Preview" />
                    </div>

                    <div class="modal-actions" v-if="!isDetailMode">
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
                    <h3>{{ foodToDelete.isActive ? "Ngưng bán" : "Bán lại" }} món ăn</h3>
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
    margin-bottom: 24px;
}

.header h2 {
    font-size: 32px;
    font-weight: 600;
    margin: 0;
    color: #2c3e50;
}

.btn-add {
    background: var(--info-color);
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
    color: #333;
    padding: 12px 16px;
    text-align: left;
    font-weight: 600;
    border-bottom: 2px solid #eee;
}

.food-table td {
    padding: 14px 12px;
    border-bottom: 1px solid #eee;
    vertical-align: middle;
    cursor: pointer;
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
    background: #ffffff1f;
    color: #333;
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-radius: 16px 16px 0 0;
    border-bottom: 2px solid #eee;
}

.btn-close {
    /* background: none; */
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
    background: var(--success-color);
    color: white;
    padding: 14px;
    border: none;
    border-radius: 12px;
    font-weight: bold;
}

.btn-cancel {
    flex: 1;
    background: var(--danger-color);
    padding: 14px;
    border: none;
    border-radius: 12px;
}
</style>