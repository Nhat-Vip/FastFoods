import Swal from "sweetalert2";

export function AlertSuccess(msg) {
    Swal.fire({
        icon: 'success',
        title: msg,
        toast: true,
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true,
        position: 'top-right'
    });
}
export function AlertError(msg) {
    Swal.fire({
        icon: 'error',
        title: msg,
        toast: true,
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true,
        position: 'top-right'
    });
}