import { createApp } from 'vue'
import App from './App.vue'
import { VueSignalR } from '@dreamonkey/vue-signalr';
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('https://localhost:7266/hub')
  .build();

createApp(App).use(VueSignalR, { connection }) .mount('#app');