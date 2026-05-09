import { createToast } from 'mosha-vue-toastify';
import 'mosha-vue-toastify/dist/style.css';
import { clearToasts } from "mosha-vue-toastify"

const toast = {
	success: (message: string, duration: number = 3000) => {
		createToast(message, {
			hideProgressBar: false,
			showIcon: true,
			position: 'bottom-center',
			type: 'success',
			transition: 'bounce',
			swipeClose: true,
			timeout: duration
		});
	},
	error: (message: string, duration: number = 3000) => {
		createToast(message, {
			hideProgressBar: false,
			showIcon: true,
			position: 'bottom-center',
			type: 'danger',
			transition: 'bounce',
			swipeClose: true,
			timeout: duration
		});
	},
	warning: (message: string, duration: number = 3000) => {
		createToast(message, {
			hideProgressBar: false,
			showIcon: true,
			position: 'bottom-center',
			type: 'warning',
			transition: 'bounce',
			swipeClose: true,
			timeout: duration
		});
	},
	info: (message: string, duration: number = 3000) => {
		createToast(message, {
			hideProgressBar: false,
			showIcon: true,
			position: 'bottom-center',
			type: 'info',
			transition: 'bounce',
			swipeClose: true,
			timeout: duration
		});
	},
	loading: (message: string) => {
		createToast(message, {
			hideProgressBar: false,
			showIcon: true,
			position: 'bottom-center',
			type: 'info',
			transition: 'bounce',
			timeout: -1,
		});
	},
	clearAll: () => {
		clearToasts()
	}
}

export default toast;