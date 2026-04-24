import { useState } from 'react';
import OrderForm from './components/OrderForm';
import OrderStatus from './components/OrderStatus';
import './App.css';

function App() {
  const [activeTab, setActiveTab] = useState('create');
  const [lastOrderId, setLastOrderId] = useState(null);

  const handleOrderSubmitted = (orderId) => {
    setLastOrderId(orderId);
    setTimeout(() => {
      setActiveTab('status');
    }, 1000);
  };

  return (
    <div className="app-container">
      <header className="app-header">
        <h1>RabbitMQ Sipariş İşleme Sistemi</h1>
        <p>Mini Mikroservis Demosu</p>
      </header>

      <nav className="tab-navigation">
        <button
          className={`tab-button ${activeTab === 'create' ? 'active' : ''}`}
          onClick={() => setActiveTab('create')}
        >
          Yeni Sipariş
        </button>
        <button
          className={`tab-button ${activeTab === 'status' ? 'active' : ''}`}
          onClick={() => setActiveTab('status')}
        >
          Sipariş Durumu
        </button>
      </nav>

      <main className="app-main">
        {activeTab === 'create' && (
          <section className="tab-content">
            <OrderForm onOrderSubmitted={handleOrderSubmitted} />
          </section>
        )}

        {activeTab === 'status' && (
          <section className="tab-content">
            <OrderStatus key={lastOrderId ?? 'status'} initialOrderId={lastOrderId} />
          </section>
        )}
      </main>

      <footer className="app-footer">
        <p>Frontend, API ile aynı origin üzerindeki <code>/api</code> proxy yolunu kullanır.</p>
      </footer>
    </div>
  );
}

export default App;
