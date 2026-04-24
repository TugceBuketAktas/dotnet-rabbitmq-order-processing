import { useEffect, useRef, useState } from 'react';
import '../styles/OrderStatus.css';

const OrderStatus = ({ initialOrderId }) => {
  const [orderId, setOrderId] = useState(() => (initialOrderId ? String(initialOrderId) : ''));
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [autoRefresh, setAutoRefresh] = useState(false);
  const refreshTimerRef = useRef(null);

  const statusLabels = {
    0: 'Bekleniyor',
    1: 'İşleniyor',
    2: 'Tamamlandı',
    3: 'Başarısız'
  };

  useEffect(() => {
    return () => {
      if (refreshTimerRef.current) {
        clearInterval(refreshTimerRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (order && (order.status === 2 || order.status === 3) && refreshTimerRef.current) {
      clearInterval(refreshTimerRef.current);
      refreshTimerRef.current = null;
      setAutoRefresh(false);
    }
  }, [order]);

  const fetchOrder = async (targetOrderId) => {
    setLoading(true);
    setMessage('');

    try {
      if (!targetOrderId) {
        setMessage('Sipariş ID gerekli');
        setOrder(null);
        return;
      }

      const response = await fetch(`/api/orders/${targetOrderId}`);

      if (!response.ok) {
        if (response.status === 404) {
          setMessage('Sipariş bulunamadı');
          setOrder(null);
        } else {
          const errorText = await response.text();
          throw new Error(errorText || 'Sipariş bilgisi alınamadı');
        }
      } else {
        const orderData = await response.json();
        setOrder(orderData);
      }
    } catch (error) {
      setMessage(`Hata: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e) => {
    e.preventDefault();
    setAutoRefresh(false);
    if (refreshTimerRef.current) {
      clearInterval(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }
    await fetchOrder(orderId);
  };

  const handleAutoRefresh = async () => {
    if (!orderId) {
      setMessage('Canlı takip için önce Sipariş ID gir');
      return;
    }

    const nextValue = !autoRefresh;
    setAutoRefresh(nextValue);

    if (!nextValue) {
      if (refreshTimerRef.current) {
        clearInterval(refreshTimerRef.current);
        refreshTimerRef.current = null;
      }
      return;
    }

    await fetchOrder(orderId);
    refreshTimerRef.current = setInterval(() => {
      fetchOrder(orderId);
    }, 2000);
  };

  return (
    <div className="order-status-container">
      <h2>Sipariş Durumunu Kontrol Et</h2>
      <p className="demo-note">
        Sipariş ID, siparişi oluşturduğunda dönen numaradır. Bu ekranda queue ve worker adımlarını durumdan takip edersin.
      </p>
      <form onSubmit={handleSearch}>
        <div className="form-group">
          <label>Sipariş ID:</label>
          <input
            type="number"
            value={orderId}
            onChange={(e) => setOrderId(e.target.value)}
            placeholder="Sipariş ID"
            disabled={loading}
          />
          <button type="submit" className="btn-search" disabled={loading}>
            {loading ? 'Aranıyor...' : 'Ara'}
          </button>
          <button type="button" className="btn-refresh" onClick={handleAutoRefresh}>
            {autoRefresh ? 'Canlı Takibi Durdur' : 'Canlı Takibi Başlat'}
          </button>
        </div>
      </form>

      {message && <div className={`message ${message.includes('Hata') ? 'error' : 'info'}`}>{message}</div>}

      {order && (
        <div className="order-details">
          <div className="detail-row">
            <span className="label">Sipariş ID:</span>
            <span className="value">{order.orderId}</span>
          </div>
          <div className="detail-row">
            <span className="label">Müşteri ID:</span>
            <span className="value">{order.customerId}</span>
          </div>
          <div className="detail-row">
            <span className="label">Toplam Tutar:</span>
            <span className="value">${order.totalAmount.toFixed(2)}</span>
          </div>
          <div className="detail-row">
            <span className="label">Durum:</span>
            <span className={`status status-${order.status}`}>{statusLabels[order.status] || 'Bilinmiyor'}</span>
          </div>
          {order.processedAt && (
            <div className="detail-row">
              <span className="label">İşlenme Zamanı:</span>
              <span className="value">{new Date(order.processedAt).toLocaleString('tr-TR')}</span>
            </div>
          )}
          <div className="detail-row">
            <span className="label">Oluşturma Tarihi:</span>
            <span className="value">{new Date(order.createdAt).toLocaleString('tr-TR')}</span>
          </div>

          <div className="flow-box">
            <div className={`flow-step ${order.status >= 0 ? 'done' : ''}`}>1) API siparişi aldı</div>
            <div className={`flow-step ${order.status >= 1 ? 'done' : ''}`}>2) RabbitMQ queue'dan worker aldı</div>
            <div className={`flow-step ${order.status >= 2 ? 'done' : order.status === 3 ? 'failed' : ''}`}>
              3) Worker işleme sonucu: {order.status === 2 ? 'Tamamlandı' : order.status === 3 ? 'Başarısız' : 'Bekliyor'}
            </div>
          </div>

          {order.items && order.items.length > 0 && (
            <div className="items-section">
              <h3>Ürünler</h3>
              <table>
                <thead>
                  <tr>
                    <th>Ürün ID</th>
                    <th>Miktar</th>
                    <th>Fiyat</th>
                    <th>Toplam</th>
                  </tr>
                </thead>
                <tbody>
                  {order.items.map((item, index) => (
                    <tr key={index}>
                      <td>{item.productId}</td>
                      <td>{item.quantity}</td>
                      <td>${item.price.toFixed(2)}</td>
                      <td>${(item.price * item.quantity).toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default OrderStatus;
