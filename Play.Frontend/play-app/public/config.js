// This file will not end up inside the main application JavaScript bundle.
// Instead, it will simply be copied inside the build folder.
// The generated "index.html" will require it just before this main bundle.
// You can thus use it to define some environment variables that will
// be made available synchronously in all your JS modules under "src".
//
// Warning: this file will not be transpiled by Babel and cannot contain
// any syntax that is not yet supported by your targeted browsers.

// window.CATALOG_SERVICE_URL = "https://localhost:7057";
// window.CATALOG_ITEMS_API_URL = `${window.CATALOG_SERVICE_URL}/items`;
// window.INVENTORY_SERVICE_URL = "https://localhost:7067";
// window.INVENTORY_ITEMS_API_URL = `${window.INVENTORY_SERVICE_URL}/items`;
// window.IDENTITY_SERVICE_URL = "https://localhost:7076";
// window.USERS_API_URL = `${window.IDENTITY_SERVICE_URL}/users`;
// window.TRADING_SERVICE_URL = "https://localhost:7264";
// window.PURCHASE_API_URL = `${window.TRADING_SERVICE_URL}/purchase`;
// window.STORE_API_URL = `${window.TRADING_SERVICE_URL}/store`;
// window.RABBITMQ_URL = "http://localhost:15672";

window.CATALOG_SERVICE_URL = "http://localhost:5002";
window.CATALOG_ITEMS_API_URL = `${window.CATALOG_SERVICE_URL}/items`;

window.INVENTORY_SERVICE_URL = "http://localhost:5003";
window.INVENTORY_ITEMS_API_URL = `${window.INVENTORY_SERVICE_URL}/items`;

window.IDENTITY_SERVICE_URL = "http://localhost:5001";
window.USERS_API_URL = `${window.IDENTITY_SERVICE_URL}/users`;

window.TRADING_SERVICE_URL = "http://localhost:5004";
window.PURCHASE_API_URL = `${window.TRADING_SERVICE_URL}/purchase`;
window.STORE_API_URL = `${window.TRADING_SERVICE_URL}/store`;

window.RABBITMQ_URL = "http://localhost:15672";
