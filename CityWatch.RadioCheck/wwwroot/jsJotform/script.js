// Open IndexedDB
let db;
const request = indexedDB.open('OfflineFormDB', 1);

request.onupgradeneeded = function (event) {
    db = event.target.result;
    const objectStore = db.createObjectStore('formData', { keyPath: 'id', autoIncrement: true });
    console.log('IndexedDB setup complete');
};

request.onsuccess = function (event) {
    db = event.target.result;
    console.log('IndexedDB opened successfully');
    displayStoredData();
};

request.onerror = function (event) {
    console.error('IndexedDB error:', event.target.error);
};

// Save Form Data to IndexedDB
document.getElementById('dataForm').addEventListener('submit', function (event) {
    event.preventDefault();
    const name = document.getElementById('name').value;
    const email = document.getElementById('email').value;

    if (name && email) {
        const transaction = db.transaction('formData', 'readwrite');
        const store = transaction.objectStore('formData');
        const data = { name, email, timestamp: new Date().toISOString() };

        const addRequest = store.add(data);

        addRequest.onsuccess = function () {
            console.log('Data saved to IndexedDB');
            displayStoredData(); // Refresh the displayed data
        };

        addRequest.onerror = function (event) {
            console.error('Error saving data:', event.target.error);
        };

        // Clear form fields
        document.getElementById('dataForm').reset();
    }
});

// Display Stored Data
function displayStoredData() {
    const transaction = db.transaction('formData', 'readonly');
    const store = transaction.objectStore('formData');
    const getAllRequest = store.getAll();

    getAllRequest.onsuccess = function (event) {
        const dataList = document.getElementById('dataList');
        dataList.innerHTML = ''; // Clear the current list

        event.target.result.forEach(data => {
            const listItem = document.createElement('li');
            listItem.textContent = `Name: ${data.name}, Email: ${data.email}, Timestamp: ${data.timestamp}`;
            dataList.appendChild(listItem);
        });
    };

    getAllRequest.onerror = function (event) {
        console.error('Error retrieving data:', event.target.error);
    };
}
