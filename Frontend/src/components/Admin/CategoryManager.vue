<template>
    <div class="category-manager">
        <div class="header">
            <h2>Quản lý danh mục món ăn</h2>
            <button @click="openCreateModal" class="btn-add">
                Thêm danh mục mới
            </button>
        </div>

        <!-- Bảng danh mục -->
        <div class="table-container">
            <table class="category-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Tên danh mục</th>
                        <th>Mô tả</th>
                        <th>Số món</th>
                        <th>Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="cat in categories" :key="cat.categoryId">
                        <td>#{{ cat.categoryId }}</td>
                        <td class="name-col">
                            <strong>{{ cat.categoryName }}</strong>
                        </td>
                        <td class="desc-col">
                            {{ cat.description || '—' }}
                        </td>
                        <td class="count-col">
                            <span class="badge">{{ cat.fastFoods?.length || 0 }} món</span>
                        </td>
                        <td class="actions">
                            <button @click="openEditModal(cat)" class="btn-edit">Sửa</button>
                            <button @click="confirmDelete(cat)" class="btn-delete">Xóa</button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal Thêm/Sửa danh mục -->
        <Teleport to="body">
            <div v-if="isModalOpen" class="modal-overlay" @click="closeModal"></div>
            <div v-if="isModalOpen" class="category-modal">
                <div class="modal-header">
                    <h3>{{ isEditMode ? 'Sửa danh mục' : 'Thêm danh mục mới' }}</h3>
                    <button @click="closeModal" class="btn-close">×</button>
                </div>

                <form @submit.prevent="saveCategory" class="modal-form">
                    <div class="form-group">
                        <label>Tên danh mục *</label>
                        <input v-model="form.categoryName" required placeholder="Ví dụ: Burger, Gà rán, Pizza..." />
                    </div>

                    <div class="form-group">
                        <label>Mô tả (không bắt buộc)</label>
                        <textarea v-model="form.description" rows="3"
                            placeholder="Mô tả ngắn về danh mục này..."></textarea>
                    </div>

                    <div class="modal-actions">
                        <button type="submit" class="btn-save">
                            {{ isEditMode ? 'Cập nhật' : 'Thêm danh mục' }}
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
                <h3>Xóa danh mục?</h3>
                <div class="confirm-actions">
                    <button @click="deleteCategory" class="btn-danger">Xóa vĩnh viễn</button>
                    <button @click="showConfirm = false" class="btn-cancel">Hủy</button>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import api from '@/api'
import { AlertError, AlertSuccess } from '@/Notification'
import { ref, onMounted } from 'vue'

const categories = ref([])

// Modal
const isModalOpen = ref(false)
const isEditMode = ref(false)
const currentCategory = ref(null)
const categoryToDelete = ref(null)
const showConfirm = ref(false)

const form = ref({
    categoryName: '',
    description: ''
})

const openCreateModal = () => {
    isEditMode.value = false
    form.value = { categoryName: '', description: '' }
    isModalOpen.value = true
}

const openEditModal = (cat) => {
    isEditMode.value = true
    currentCategory.value = cat
    form.value = {
        categoryName: cat.categoryName,
        description: cat.description || ''
    }
    isModalOpen.value = true
}

const closeModal = () => {
    isModalOpen.value = false
    currentCategory.value = null
}

const saveCategory = async () => {
    try {

        if (form.value.categoryName.trim() === '') {
            AlertError('Vui lòng nhập tên danh mục!')
            return
        }

        if (isEditMode.value) {
            await api.put(`/categories/${currentCategory.value.categoryId}`, form.value);

            currentCategory.value.categoryName = form.value.categoryName
            currentCategory.value.description = form.value.description
            AlertSuccess('Cập nhật danh mục thành công!')
        } else {
            const res = await api.post("/categories", form.value);

            categories.value.push({
                categoryId: res.data.categoryId,
                categoryName: form.value.categoryName,
                description: form.value.description,
                foodCount: 0,
                isActive: true
            })
            AlertSuccess('Thêm danh mục thành công!')
        }
        closeModal()
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra vui lòng thử lại sau");
            console.log(e.response);
        }
    }
}


const confirmDelete = (cat) => {
    categoryToDelete.value = cat
    showConfirm.value = true
}

const deleteCategory = async () => {
    try
    {
        if (categoryToDelete.value.fastFoods?.length > 0) {
            AlertError(
                `Không thể xóa danh mục "${categoryToDelete.value.categoryName}" vì vẫn còn 
                ${categoryToDelete.value.fastFoods.length} món ăn thuộc danh mục này`
            );
            return;
        }
        await api.delete(`/categories/${categoryToDelete.value.categoryId}`);
        categories.value = categories.value.filter(c => c.categoryId !== categoryToDelete.value.categoryId)
        AlertSuccess('Đã xóa danh mục!')
        showConfirm.value = false
        categoryToDelete.value = null
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra khi xóa sản phẩm vui lòng thử lại sau");
            console.log(e.response);
        }
    }
}

onMounted( async() => {
    const res = await api.get("/categories");
    categories.value = res.data;
})
</script>

<style scoped>
.category-manager {
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
}

.category-table {
    width: 100%;
    border-collapse: collapse;
}

.category-table th {
    background: #2c3e50;
    color: white;
    padding: 16px 12px;
    text-align: left;
}

.category-table td {
    padding: 16px 12px;
    border-bottom: 1px solid #eee;
    vertical-align: middle;
}

.category-table tr:hover {
    background: #f8f9fa;
}

.name-col strong {
    font-size: 16px;
}

.desc-col {
    color: #666;
    max-width: 300px;
}

.count-col .badge {
    background: #3498db;
    color: white;
    padding: 4px 12px;
    border-radius: 20px;
    font-size: 13px;
    font-weight: bold;
}

.status-badge {
    padding: 6px 14px;
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

/* Modal */
.modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.6);
    z-index: 999;
}

.category-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 90%;
    max-width: 550px;
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
    padding: 30px;
}

.form-group {
    margin-bottom: 20px;
}

.form-group label {
    display: block;
    margin-bottom: 8px;
    font-weight: 600;
    color: #2c3e50;
}

.form-group input,
.form-group textarea {
    width: 100%;
    padding: 12px;
    border: 1px solid #ddd;
    border-radius: 8px;
    font-size: 15px;
}

.modal-actions {
    display: flex;
    gap: 12px;
    margin-top: 30px;
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

/* Confirm modal */
.confirm-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: white;
    padding: 30px;
    border-radius: 16px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.4);
    z-index: 1000;
    max-width: 420px;
    text-align: center;
}

.warning {
    color: #e74c3c;
    font-size: 14px;
    margin: 12px 0;
}

.confirm-actions {
    margin-top: 20px 0 0;
    display: flex;
    gap: 12px;
}

.btn-danger {
    flex: 1;
    background: #e74c3c;
    color: white;
    padding: 12px;
    border: none;
    border-radius: 8px;
}
</style>