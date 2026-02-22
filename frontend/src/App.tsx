import { useState, useCallback } from 'react';
import { Product, Cart, Order } from './types';
import ProductList from './components/ProductList';
import CartPage from './components/CartPage';
import OrderConfirmation from './components/OrderConfirmation';
import { cartApi } from './services/api';

type View = 'products' | 'cart' | 'order';

function App() {
  const [view, setView] = useState<View>('products');
  const [cartId, setCartId] = useState<string | null>(null);
  const [cartItemCount, setCartItemCount] = useState(0);
  const [completedOrder, setCompletedOrder] = useState<Order | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const showToast = useCallback((msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2500);
  }, []);

  const handleAddToCart = useCallback((newCartId: string) => {
    setCartId(newCartId);
    setCartItemCount(prev => prev + 1);
    showToast('Added to cart!');
  }, [showToast]);

  const handleCartUpdate = useCallback((cart: Cart) => {
    setCartItemCount(cart.items.reduce((sum, item) => sum + item.quantity, 0));
  }, []);

  const handleCheckoutComplete = useCallback((order: Order) => {
    setCompletedOrder(order);
    setCartId(null);
    setCartItemCount(0);
    setView('order');
  }, []);

  const handleContinueShopping = useCallback(() => {
    setCompletedOrder(null);
    setView('products');
  }, []);

  return (
    <div className="app">
      <header className="header">
        <h1>🛒 Smart Cart</h1>
        <div className="nav-buttons">
          <button
            className={`nav-btn ${view === 'products' ? 'active' : ''}`}
            onClick={() => setView('products')}
          >
            Products
          </button>
          <button
            className={`nav-btn ${view === 'cart' ? 'active' : ''}`}
            onClick={() => setView('cart')}
          >
            Cart
            {cartItemCount > 0 && <span className="cart-badge">{cartItemCount}</span>}
          </button>
        </div>
      </header>

      {view === 'products' && (
        <ProductList cartId={cartId} onAddToCart={handleAddToCart} />
      )}

      {view === 'cart' && (
        <CartPage
          cartId={cartId}
          onCartUpdate={handleCartUpdate}
          onCheckoutComplete={handleCheckoutComplete}
          onGoToProducts={() => setView('products')}
        />
      )}

      {view === 'order' && completedOrder && (
        <OrderConfirmation
          order={completedOrder}
          onContinueShopping={handleContinueShopping}
        />
      )}

      {toast && <div className="toast">{toast}</div>}
    </div>
  );
}

export default App;
