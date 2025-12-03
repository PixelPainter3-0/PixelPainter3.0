import "./assets/main.css";

// Vue 3
import { createPinia } from "pinia";
import { createApp } from "vue";
import App from "./App.vue";

import router from "./router";

//PrimeVue
import PrimeVue from "primevue/config";
import "primeicons/primeicons.css";
import Aura from "@primevue/themes/aura";
import ToastService from "primevue/toastservice";
import ConfirmationService from "primevue/confirmationservice";

//PrimeFlex
import "primeflex/primeflex.css";

import Tooltip from "primevue/tooltip"

const pinia = createPinia();

createApp(App)
  .use(PrimeVue, {
    theme: {
      preset: Aura,
      options: {
        darkModeSelector: ".dark-mode-toggle",
      },
    },
  })
  .use(router)
  .use(ToastService)
  .use(ConfirmationService)
  .use(pinia)
  .directive('tooltip', Tooltip)
  .mount("#app");
