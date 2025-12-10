<script setup>
import api from '@/api'
import { AlertError, AlertSuccess } from '@/Notification'
import useVuelidate from '@vuelidate/core'
import { helpers, required } from '@vuelidate/validators'
import { onMounted, ref } from 'vue'

const users = ref([])

// Modal
const isModalOpen = ref(false)
const isEditMode = ref(false)
const currentUser = ref(null)
const userToDelete = ref(null)
const showConfirm = ref(false)

const form = ref({
    userId: null,
    username: '',
    email: '',
    fullName: '',
    phone: '',
    address: '',
    dateOfBirth: null,
    userRole: 1,
    password: ''
})

const phoneValidator = helpers.withMessage(
    'Số điện thoại không hợp lệ',
    value => /^0\d{9}$/.test(value)
);

const rules = {
    form: {
        phone: {
            required: helpers.withMessage('Vui lòng nhập số điện thoại', required),
            phoneValidator
        }
    }
}

const v$ = useVuelidate(rules, { form });

// Mở modal thêm
const openCreateModal = () => {
    isEditMode.value = false
    form.value = {userId: null, username: '', email: '', fullName: '', phone: '', address: '', dateOfBirth: null, userRole: 1, password: '' }
    isModalOpen.value = true
}

// Mở modal sửa
const openEditModal = (user) => {
    isEditMode.value = true
    currentUser.value = user
    form.value = {
        userId: user.userId,
        username: user.username,
        email: user.email,
        fullName: user.fullName,
        phone: user.phone,
        address: user.address,
        dateOfBirth: user.dateOfBirth,
        userRole: user.userRole,
        password: ''
    }
    isModalOpen.value = true
}


const closeModal = () => {
    isModalOpen.value = false
    currentUser.value = null
}

// Lưu (thêm/sửa)
const saveUser = async () => {
    try {
        const isValid = v$.value.form.$validate();
        if (!isValid) return;

        if (isEditMode.value) {
            Object.assign(currentUser.value, {
                username: form.value.username,
                fullName: form.value.fullName,
                email: form.value.email,
                phone: form.value.phone,
                address: form.value.address,
                dateOfBirth: form.value.dateOfBirth,
                userRole: form.value.userRole
            });
            await api.put(`/users/${currentUser.value.userId}`, form.value);

            AlertSuccess('Cập nhật thành công!');
        } else {
            const newUser = {
                userId: Math.max(...users.value.map(u => u.userId)) + 1,
                username: form.value.username,
                email: form.value.email,
                fullName: form.value.fullName,
                phone: form.value.phone,
                address: form.value.address,
                dateOfBirth: form.value.dateOfBirth,
                userRole: form.value.userRole,
                isActive: true,
                createdAt: new Date().toISOString(),
                passwordHash: form.value.password
            }
            const res = await api.post("/users", newUser);
            users.value.push(res.data);
            AlertSuccess("Thêm người dùng mới thành công!");
        }
        closeModal();
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra vui lòng thử lại");
            console.log("Có lỗi xảy ra", e);
        }
    }
}

// Xóa + Khóa/Mở
const confirmDelete = (user) => { userToDelete.value = user; showConfirm.value = true }
const deleteUser = async() => {
    try {

        await api.delete(`/users/${userToDelete.value.userId}`);

        users.value = users.value.filter(u => u.userId !== userToDelete.value.userId)
        AlertSuccess('Đã xóa người dùng!')
        showConfirm.value = false;
    }
    catch (e) {
        if (e.response && e.response.status === 400) {
            AlertError(e.response.data.message);
        }
        else {
            AlertError("Có lỗi xảy ra vui lòng thử lại");
        }
    }
}
const toggleActive = async(user) => {
    await api.put(`/users/lock/${user.userId}`);
    user.isActive = !user.isActive
    AlertSuccess(user.isActive ? 'Đã mở khóa' : 'Đã khóa tài khoản')
}

// Helper
const formatDate = (date) => new Date(date).toLocaleDateString('vi-VN');


onMounted(async() => {
    const res = await api.get("/users");
    users.value = res.data;
})
</script>
<template>
    <div class="user-manager">
        <div class="header">
            <h2>Quản lý người dùng</h2>
            <button @click="openCreateModal" class="btn-add">
                Thêm người dùng
            </button>
        </div>

        <!-- Bảng danh sách -->
        <div class="table-container">
            <table class="user-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Tên đăng nhập</th>
                        <th>Họ tên</th>
                        <th>Email</th>
                        <th>SĐT</th>
                        <th>Vai trò</th>
                        <th>Trạng thái</th>
                        <th>Ngày tạo</th>
                        <th>Hành động</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="user in users" :key="user.userId">
                        <td>#{{ user.userId }}</td>
                        <td><strong>{{ user.username }}</strong></td>
                        <td>
                            <div class="user-info">
                                {{ user.fullName }}
                            </div>
                        </td>
                        <td>{{ user.email }}</td>
                        <td>{{ user.phone }}</td>
                        <td>
                            <span class="role-badge" :class="user.userRole === 2 ? 'admin' : 'customer'">
                                {{ user.userRole === 2 ? 'Quản trị viên' : 'Khách hàng' }}
                            </span>
                        </td>
                        <td>
                            <span class="status-badge" :class="user.isActive ? 'active' : 'inactive'">
                                {{ user.isActive ? 'Hoạt động' : 'Bị khóa' }}
                            </span>
                        </td>
                        <td>{{ formatDate(user.createdAt) }}</td>
                        <td class="actions">
                            <button @click="openEditModal(user)" class="btn-edit" title="Sửa">Sửa</button>
                            <button @click="toggleActive(user)" class="btn-toggle"
                                :class="user.isActive ? 'lock' : 'unlock'">
                                {{ user.isActive ? 'Khóa' : 'Mở' }}
                            </button>
                            <button @click="confirmDelete(user)" class="btn-delete" title="Xóa">Xóa</button>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Modal Thêm / Sửa -->
        <Teleport to="body">
            <div v-if="isModalOpen" class="modal-overlay" @click="closeModal"></div>
            <div v-if="isModalOpen" class="user-modal">
                <div class="modal-header">
                    <h3>{{ isEditMode ? 'Sửa người dùng' : 'Thêm người dùng mới' }}</h3>
                    <button @click="closeModal" class="btn-close">×</button>
                </div>

                <form @submit.prevent="saveUser" class="modal-form">
                    <div class="form-row">
                        <div class="form-group">
                            <label>Tên đăng nhập *</label>
                            <input v-model="form.username" required />
                        </div>
                        <div class="form-group">
                            <label>Họ và tên *</label>
                            <input v-model="form.fullName" required />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Email *</label>
                            <input v-model="form.email" type="email" required />
                        </div>
                        <div class="form-group">
                            <label>Số điện thoại *</label>
                            <input v-model="form.phone" maxlength="11" @blur="v$.form.phone.$touch" required />
                            <div v-if="v$.form.phone.$error" class="text-danger">
                                {{ v$.form.phone.$errors[0].$message }}
                            </div>
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Địa chỉ *</label>
                        <input v-model="form.address" required />
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label>Ngày sinh</label>
                            <input v-model="form.dateOfBirth" type="date" />
                        </div>
                        <div class="form-group">
                            <label>Vai trò *</label>
                            <select v-model="form.userRole">
                                <option :value="1">Khách hàng</option>
                                <option :value="2">Quản trị viên</option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group" v-if="!isEditMode">
                        <label>Mật khẩu *</label>
                        <input v-model="form.password" type="password" required />
                    </div>

                    <div class="form-group" v-else>
                        <label>Mật khẩu mới (để trống nếu không đổi)</label>
                        <input v-model="form.password" type="password" placeholder="Để trống nếu không đổi" />
                    </div>

                    <div class="modal-actions">
                        <button type="submit" class="btn-save">
                            {{ isEditMode ? 'Cập nhật' : 'Thêm mới' }}
                        </button>
                        <button type="button" @click="closeModal" class="btn-cancel">Hủy</button>
                    </div>
                </form>
            </div>
        </Teleport>

        <!-- Confirm xóa -->
        <Teleport to="body">
            <div v-if="showConfirm" class="modal-overlay" @click="showConfirm = false"></div>
            <div v-if="showConfirm" class="confirm-modal">
                <h3>Xác nhận xóa</h3>
                <p>Bạn có chắc chắn muốn xóa người dùng <strong>{{ userToDelete?.fullName }}</strong> không?</p>
                <div class="confirm-actions">
                    <button @click="deleteUser" class="btn-danger">Xóa vĩnh viễn</button>
                    <button @click="showConfirm = false" class="btn-cancel ">Hủy</button>
                </div>
            </div>
        </Teleport>
    </div>
</template>
<style scoped>
.form-row {
    display: flex;
    gap: 16px;
}

.form-row .form-group {
    flex: 1;
}

.user-info {
    display: flex;
    align-items: center;
    gap: 10px;
}

.avatar {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    object-fit: cover;
}

.user-manager {
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
    margin: 0;
    color: #2c3e50;
}

.btn-add {
    background: #2874f0;
    color: white;
    padding: 10px 20px;
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
    max-height: 500px;
    overflow-y: auto;
}

.user-table {
    width: 100%;
    border-collapse: collapse;
    table-layout: fixed;
}

/* .user-table th,
.user-table td {
    overflow: hidden; 
    text-overflow: ellipsis;
    white-space: wrap;
    Không cho xuống dòng
} */

/* Cố định chiều rộng từng cột */
.user-table th:nth-child(1),
.user-table td:nth-child(1) {
    width: 60px;
}

/* ID */
.user-table th:nth-child(2),
.user-table td:nth-child(2) {
    width: max-content;
}

/* Tên đăng nhập */
.user-table th:nth-child(3),
.user-table td:nth-child(3) {
    width: max-content;
}

/* Họ tên + avatar */
.user-table th:nth-child(4),
.user-table td:nth-child(4) {
    width: 180px;
}

/* Email */
.user-table th:nth-child(5),
.user-table td:nth-child(5) {
    width: 120px;
}

/* SĐT */
.user-table th:nth-child(6),
.user-table td:nth-child(6) {
    width: 100px;
}

/* Vai trò */
.user-table th:nth-child(7),
.user-table td:nth-child(7) {
    width: 120px;
}

/* Trạng thái */
.user-table th:nth-child(8),
.user-table td:nth-child(8) {
    width: 100px;
}

/* Ngày tạo */
.user-table th:nth-child(9),
.user-table td:nth-child(9) {
    width: 180px;
    text-align: center;
}

/* Hành động */

.user-table th {
    background: #2c3e50;
    color: white;
    padding: 5px 12px;
    text-align: left;
}

.user-table td {
    padding: 10px 5px;
    border-bottom: 1px solid #eee;
}

.user-table tr:hover {
    background: #f8f9fa;
}

.user-info {
    display: flex;
    align-items: center;
    gap: 12px;
}

.avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    object-fit: cover;
}

.role-badge {
    padding: 4px 12px;
    border-radius: 20px;
    font-size: 12px;
    font-weight: bold;
    white-space: nowrap;
}

.role-badge.admin {
    background: #e74c3c;
    color: white;
}

.role-badge.customer {
    background: #27ae60;
    color: white;
}

.status-badge {
    padding: 4px 12px;
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
    cursor: pointer;
    font-size: 12px;
}

.btn-edit {
    background: #3498db;
    color: white;
}

.btn-delete {
    background: #e74c3c;
    color: white;
}

.btn-toggle.lock {
    background: #95a5a6;
    color: white;
}

.btn-toggle.unlock {
    background: #27ae60;
    color: white;
}



.user-modal {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: white;
    width: 90%;
    max-width: 500px;
    border-radius: 16px;
    z-index: 1000;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
    background: #2c3e50;
    color: white;
    padding: 16px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-radius: 16px 16px 0 0;
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
}

.form-group {
    margin-bottom: 16px;
}

.form-group label {
    display: block;
    margin-bottom: 6px;
    font-weight: 600;
}

.form-group input,
.form-group select {
    width: 100%;
    padding: 12px;
    border: 1px solid #ddd;
    border-radius: 8px;
    font-size: 15px;
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
    border-radius: 8px;
    font-weight: bold;
}

.btn-cancel {
    flex: 1;
    background: #c31616c8;
    padding: 14px;
    border: none;
    font-weight: bold;
    color: white;
    border-radius: 8px;
}

</style>