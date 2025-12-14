<script setup>
import api from '@/api'
import { AlertError, AlertSuccess } from '@/Notification'
import { ref, onMounted } from 'vue'

const combos = ref([])

const fastFoods = ref([
    { id: 1, name: "Gà rán" },
    { id: 2, name: "Khoai tây chiên lớn" },
    { id: 3, name: "Pepsi" },
    { id: 4, name: "Burger bò phô mai" }
])

const isModalOpen = ref(false)
const isEditMode = ref(false)
const currentCombo = ref(null)
const comboToDelete = ref(null)
const showConfirm = ref(false)
const previewUrl = ref('');
const selectedFile = ref(null);
const isLockCombo = ref(false);

const form = ref({
    name: '',
    description: '',
    price: 0,
    imageFile: null,
    comboItems: []
})

const openCreateModal = () => {
    isEditMode.value = false
    form.value = { name: '', description: '', price: 0, comboItems: [{ fastFoodId: '', quantity: 1 }] }
    previewUrl.value = ''
    isModalOpen.value = true
}

const openEditModal = (combo) => {
    isEditMode.value = true
    currentCombo.value = combo
    form.value = {
        name: combo.name,
        description: combo.description,
        price: combo.price,
        imageUrl: combo.imageUrl,
        comboItems: combo.comboItems.map(i => ({ ...i }))
    }
    previewUrl.value = combo.imageUrl || ''
    isModalOpen.value = true
}

const closeModal = () => { isModalOpen.value = false }

const onFileChange = (e) => {
    const file = e.target.files[0]
    if (file) {
        selectedFile.value = file;
        previewUrl.value = URL.createObjectURL(file)
    }
}

const mapToComboItem = (item) => {
    return {
        FastFoodId: item.fastFoodId,
        Quantity: item.quantity
    }
}

const saveCombo = async () => {
    try {
        const processedItems = form.value.comboItems.map(item => mapToComboItem(item));
        if (form.value.comboItems.length == 0) {
            AlertError("Phải có ít nhất 1 sản phẩm trong combo");
            return;
        }

        if (isEditMode.value) {

            const formData = new FormData();
            formData.append("Name", form.value.name);
            formData.append("Price", form.value.price);
            formData.append("Description", form.value.description);
            formData.append("ComboItems", JSON.stringify(processedItems));
            if (selectedFile.value) {
                formData.append("ImageFile", selectedFile.value);
            }

            const res = await api.put(`/combos/${currentCombo.value.comboId}`, formData, {
                headers: {
                    "Content-Type": "multipart/form-data"
                }
            });
            console.log("Cập nhật Combo: ", res.data);

            Object.assign(currentCombo.value, res.data);
            AlertSuccess('Cập nhật thành công!')
        } else {
            if (!selectedFile.value) {
                AlertError("Vui lòng chọn ảnh cho combo");
                return;
            }
            const formData = new FormData();
            formData.append("Name", form.value.name);
            formData.append("Price", form.value.price);
            formData.append("Description", form.value.description);
            formData.append("ComboItems", JSON.stringify(processedItems));
            formData.append("ImageFile", selectedFile.value);

            const res = await api.post("/combos", formData, {
                headers: {
                    "Content-Type": "multipart/form-data"
                }
            });
            combos.value.push(res.data);
            AlertSuccess('Thêm Combo thành công!')
        }
        closeModal()
    }
    catch (e) {
        console.log("Có lỗi xảy ra",e);   
    }
}

const toggleActive = (combo) => {
    comboToDelete.value = combo
    showConfirm.value = true;
    isLockCombo.value = true;
}

const lockCombo = async () => {
    try {
        const res = await api.put(`/combos/lock/${comboToDelete.value.comboId}`);
        console.log("Lock Combo: ", res.data);
        comboToDelete.value.isActive = !comboToDelete.value.isActive;
        showConfirm.value = false
        AlertSuccess(`${comboToDelete.value.isActive ? "Bán lại": "Ngưng bán"} ${comboToDelete.value.name} thành công`);
        comboToDelete.value = null
    }
    catch (e) {
        console.log("Có lỗi xảy ra", e);
        AlertError("Có lỗi đã xảy ra");
    }
}
const confirmDelete = (combo) => {
    comboToDelete.value = combo
    showConfirm.value = true
    isLockCombo.value = false
}

const deleteCombo = async () => {
    try {

        await api.delete(`/combos/${comboToDelete.value.comboId}`);
        combos.value = combos.value.filter(c => c.comboId !== comboToDelete.value.comboId)
        showConfirm.value = false
        comboToDelete.value = null
    }
    catch (e) {
        console.log("Có lỗi xảy ra", e.response);
        AlertError("Có lỗi xảy ra");
    }
}

const formatPrice = (p) => p.toLocaleString('vi-VN')
const formatDate = (d) => new Date(d).toLocaleDateString('vi-VN')

onMounted(async () => {
    const resCombo = await api.get("/combos/admin");
    combos.value = resCombo.data;

    const resFood = await api.get("/fastfoods");
    fastFoods.value = resFood.data;

})
</script>
<template>
    <div class="combo-manager">
        <div class="header">
            <h2>Quản lý Combo</h2>
            <button @click="openCreateModal" class="btn-add">
                Thêm Combo mới
            </button>
        </div>

        <!-- Bảng Combo -->
        <div class="table-container">
            <table class="combo-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Ảnh</th>
                        <th>Tên Combo</th>
                        <th>Mô tả</th>
                        <th>Các món</th>
                        <th>Giá</th>
                        <th>Trạng thái</th>
                        <th>Ngày tạo</th>
                        <th>Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="combo in combos" :key="combo.comboId">
                        <td>#{{ combo.comboId }}</td>
                        <td>
                            <img :src="combo.imageUrl || 'https://via.placeholder.com/80x60?text=No+Image'" alt="combo"
                                class="thumb" />
                        </td>
                        <td class="name-col">
                            <strong>{{ combo.name }}</strong>
                        </td>
                        <td class="desc-col" :title="combo.description">{{ combo.description }}</td>
                        <td class="items-col">
                            <div v-for="item in combo.comboItems.slice(0,3)" :key="item.fastFoodId" class="item-tag">
                                {{ item.fastFood.name }} ×{{ item.quantity }}
                            </div>
                            <!-- Nếu có hơn 3 món → hiện dấu ... -->
                            <div v-if="combo.comboItems.length > 3" class="more-items">
                                +{{ combo.comboItems.length - 3 }} món nữa
                            </div>
                        </td>
                        <td class="price-col">{{ formatPrice(combo.price) }}₫</td>
                        <td>
                            <span class="status-badge" :class="combo.isActive ? 'active' : 'inactive'">
                                {{ combo.isActive ? 'Đang bán' : 'Ngưng bán' }}
                            </span>
                        </td>
                        <td>{{ formatDate(combo.createdAt) }}</td>
                        <td class="actions">
                            <button @click="openEditModal(combo)" class="btn-edit" title="Sửa">Sửa</button>
                            <button @click="confirmDelete(combo)" class="btn-delete" title="Xóa">Xóa</button>
                            <button @click="toggleActive(combo)" class="btn-toggle"
                                :class="combo.isActive ? 'hide' : 'show'">
                                {{ combo.isActive ? 'Ngưng bán' : 'Bán' }}
                            </button>

                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal Thêm/Sửa Combo -->
        <Teleport to="body">
            <div v-if="isModalOpen" class="modal-overlay" @click="closeModal"></div>
            <div v-if="isModalOpen" class="combo-modal">
                <div class="modal-header">
                    <h3>{{ isEditMode ? 'Sửa Combo' : 'Thêm Combo mới' }}</h3>
                    <button @click="closeModal" class="btn-close">×</button>
                </div>

                <form @submit.prevent="saveCombo" class="modal-form">
                    <div class="form-row">
                        <div class="form-group">
                            <label>Tên Combo *</label>
                            <input class="form-control" v-model="form.name" required />
                        </div>
                        <div class="form-group">
                            <label>Giá (₫) *</label>
                            <input class="form-control" v-model.number="form.price" type="number" min="0" required />
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Mô tả *</label>
                        <textarea v-model="form.description" rows="3" required></textarea>
                    </div>

                    <div class="form-group">
                        <label class="form-label">Ảnh Combo (URL)</label>
                        <input type="file" @change="onFileChange" accept="image/*" class="mt-2 form-control" />
                        <img v-if="previewUrl" :src="previewUrl" class="preview-img" />
                    </div>

                    <!-- Danh sách món trong Combo -->
                    <div class="items-section">
                        <h4>Các món trong Combo</h4>
                        <div v-for="(item, i) in form.comboItems" :key="i" class="item-row">
                            <select class="form-select" v-model="item.fastFoodId" required>
                                <option value="">-- Chọn món --</option>
                                <option v-for="food in fastFoods" :value="food.fastFoodId">{{ food.name }}</option>
                            </select>
                            <input v-model.number="item.quantity" type="number" min="1" placeholder="SL"
                                class="form-control qty-input" />
                            <button type="button" @click="form.comboItems.splice(i, 1)" class="btn-remove">×</button>
                        </div>
                        <button type="button" @click="form.comboItems.push({ fastFoodId: '', quantity: 1 })"
                            class="btn-add-item">
                            + Thêm món
                        </button>
                    </div>

                    <div class="modal-actions">
                        <button type="submit" class="btn-save">
                            {{ isEditMode ? 'Cập nhật' : 'Thêm Combo' }}
                        </button>
                        <button type="button" @click="closeModal" class="btn-cancel">Hủy</button>
                    </div>
                </form>
            </div>
        </Teleport>

        <!-- Confirm Delete -->
        <Teleport to="body">
            <div v-if="showConfirm" class="modal-overlay" @click="showConfirm = false"></div>
            <div v-if="showConfirm" class="confirm-modal">
                <div v-if="!isLockCombo">
                    <h3>Xóa Combo?</h3>
                    <p>Xóa <strong>{{ comboToDelete?.name }}</strong> vĩnh viễn?</p>
                </div>
                <div v-else>
                    <h3>Ngưng bán Combo?</h3>
                    <p>Ngưng bán <strong>{{ comboToDelete?.name }}</strong></p>
                </div>
                <div class="confirm-actions">
                    <button v-if="!isLockCombo" @click="deleteCombo" class="btn-danger">Xóa</button>
                    <button v-else @click="lockCombo" class="btn-danger">
                        {{ comboToDelete.isActive ? "Ngưng bán" : "Bán lại"}}
                    </button>
                    <button @click="showConfirm = false" class="btn-cancel">Hủy</button>
                </div>
            </div>
        </Teleport>
    </div>
</template>
<style scoped>
.combo-manager {
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
    background: #2874f0;
    color: white;
    padding: 12px 24px;
    border: none;
    border-radius: 8px;
    font-weight: bold;
}

.table-container {
    background: white;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.08);
    max-height: 500px;
    overflow-y: auto;
}

.combo-table {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
}
/* ID */
.combo-table th:nth-child(1),
.combo-table td:nth-child(1) {
    width: 40px;
}
/* Ảnh */
.combo-table th:nth-child(2),
.combo-table td:nth-child(2) {
    width: 100px;
}
/* Tên combo*/
.combo-table th:nth-child(3),
.combo-table td:nth-child(3) {
    width: 140px;
}
/* Mô tả */
.combo-table th:nth-child(4),
.combo-table td:nth-child(4) {
    text-align: center;
    width: 160px;
}
/* Các món */
.combo-table th:nth-child(5),
.combo-table td:nth-child(5) {
    text-align: center;
    width: 140px;
}
/* Giá */
.combo-table th:nth-child(6),
.combo-table td:nth-child(6) {
    text-align: center;
    width: 100px;
}
/* Trạng thái */
.combo-table th:nth-child(7),
.combo-table td:nth-child(7) {
    width: 90px;
}
/* Ngày tạo */
.combo-table th:nth-child(8),
.combo-table td:nth-child(8) {
    width: 90px;
}
/* Hành động */
.combo-table th:nth-child(9),
.combo-table td:nth-child(9) {
    text-align: center;
    width: 140px;
}

.combo-table th {
    background: #2c3e50;
    color: white;
    padding: 12px 12px;
    text-align: left;
    font-weight: 600;
}

.combo-table td {
    padding: 14px 12px;
    /* border-bottom: 1px solid #eee; */
    vertical-align: middle;
}
.combo-table tr{
    border-bottom: 1px solid #eee;
}

.combo-table tr:hover {
    background: #f8f9fa;
}

.thumb {
    width: 80px;
    height: 60px;
    object-fit: cover;
    border-radius: 8px;
}

.name-col {
    font-weight: 600;
}

.desc-col {
    max-width: 200px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: #555;
}

.items-col {
    max-width: 220px;
}

.item-tag {
    display: inline-block;
    background: #e3f2fd;
    color: #1976d2;
    padding: 4px 10px;
    border-radius: 20px;
    font-size: 12px;
    margin: 2px;

}

.more-items {
    color: #666;
    font-size: 13px;
    font-style: italic;
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
    white-space: nowrap;
}

.status-badge.active {
    background: #d4edda;
    color: #155724;
}

.status-badge.inactive {
    background: #f8d7da;
    color: #721c24;
}

.actions{
    display: flex;
    width: 100%;
    justify-content: center;
    flex-wrap: wrap;
    gap: 2px;
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

.btn-toggle.hide {
    background: #95a5a6;
    color: white;
}

.btn-toggle.show {
    background: #27ae60;
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

.combo-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 90%;
    max-width: 700px;
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
    /* padding: 12px; */
    border: 1px solid #ddd;
    border-radius: 8px;
}

.preview-img {
    width: 100%;
    max-height: 180px;
    object-fit: cover;
    margin-top: 10px;
    border-radius: 8px;
}

.items-section h4 {
    margin: 24px 0 12px;
}

.item-row {
    display: flex;
    gap: 12px;
    align-items: center;
    margin-bottom: 12px;
}

.item-row select {
    flex: 2;
}

.qty-input {
    width: 80px;
}

.btn-remove {
    background: #e74c3c;
    color: white;
    border: none;
    width: 36px;
    height: 36px;
    border-radius: 50%;
    cursor: pointer;
}

.btn-add-item {
    background: #2874f0;
    color: white;
    padding: 10px 16px;
    border: none;
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
    background: #c31616c8;
    padding: 14px;
    border: none;
    color: white;
    font-weight: bold;
    border-radius: 12px;
}
</style>