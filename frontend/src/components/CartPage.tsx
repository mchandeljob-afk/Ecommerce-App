import { useState, useEffect, useCallback } from 'react';
import { Cart, Order } from '../types';
import { cartApi } from '../services/api';
import axios from 'axios';

interface Props {
  cartId: string | null;
  onCartUpdate: (cart: Cart) => void;
  onCheckoutComplete: (order: Order) => void;
  onGoToProducts: () => void;
}

export default function CartPage({ cartId, onCartUpdate, onCheckoutComplete, onGoToProducts }: Props) {
  const [cart, setCart] = useState<Cart | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [couponCode, setCouponCode] = useState('');
  const [couponMsg, setCouponMsg] = useState<{ text: string; type: 'success' | 'error' } | null>(null);
  const [applyingCoupon, setApplyingCoupon] = useState(false);
  const [checkingOut, setCheckingOut] = useState(false);

  const loadCart = useCallback(async () => {
    if (!cartId) {
      setLoading(false);
      return;
    }
    try {
      setLoading(true);
      setError(null);
      const data = await cartApi.getCart(cartId);
      setCart(data);
      onCartUpdate(data);
    } catch (err) {
      setError('Failed to load cart.');
    } finally {
      setLoading(false);
    }
  }, [cartId, onCartUpdate]);

  useEffect(() => {
    loadCart();
  }, [loadCart]);

  const handleApplyCoupon = async () => {
    if (!cartId || !couponCode.trim()) return;
    try {
      setApplyingCoupon(true);
      setCouponMsg(null);
      const result = await cartApi.applyCoupon(cartId, couponCode.trim());
      setCouponMsg({ text: `${result.message} Discount: ₹${result.discountAmount.toFixed(2)}`, type: 'success' });
      await loadCart();
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.error) {
        setCouponMsg({ text: err.response.data.error, type: 'error' });
      } else {
        setCouponMsg({ text: 'Failed to apply coupon.', type: 'error' });
      }
    } finally {
      setApplyingCoupon(false);
    }
  };

  const handleCheckout = async () => {
    if (!cartId) return;
    try {
      setCheckingOut(true);
      setError(null);
      const order = await cartApi.checkout(cartId);
      onCheckoutComplete(order);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.error) {
        setError(err.response.data.error);
      } else {
        setError('Checkout failed. Please try again.');
      }
    } finally {
      setCheckingOut(false);
    }
  };

  if (loading) {
    return (
      <div className="loading">
        <div className="spinner" />
        <p>Loading cart...</p>
      </div>
    );
  }

  if (!cartId || !cart || cart.items.length === 0) {
    return (
      <div className="empty-state">
        <h3>Your cart is empty</h3>
        <p>Add some products to get started.</p>
        <button className="add-btn" style={{ maxWidth: 200, marginTop: 16 }} onClick={onGoToProducts}>
          Browse Products
        </button>
      </div>
    );
  }

  return (
    <div className="cart-container">
      <div className="cart-items">
        <h2>Shopping Cart ({cart.items.length} items)</h2>
        {cart.items.map(item => (
          <div key={item.id} className="cart-item">
            <div className="cart-item-info">
              <h3>{item.productName}</h3>
              <p>₹{item.unitPrice.toFixed(2)} each</p>
            </div>
            <div className="qty-controls">
              <span style={{ fontWeight: 600 }}>Qty: {item.quantity}</span>
            </div>
            <div className="cart-item-total">₹{item.lineTotal.toFixed(2)}</div>
          </div>
        ))}
      </div>

      <div className="cart-summary">
        <h2>Order Summary</h2>

        <div className="summary-row">
          <span>Subtotal</span>
          <span>₹{cart.subtotal.toFixed(2)}</span>
        </div>

        {cart.discountAmount > 0 && (
          <div className="summary-row discount">
            <span>Discount ({cart.appliedCouponCode})</span>
            <span>-₹{cart.discountAmount.toFixed(2)}</span>
          </div>
        )}

        <div className="summary-row">
          <span>Tax (18% GST)</span>
          <span>₹{(Math.max(0, cart.subtotal - cart.discountAmount) * 0.18).toFixed(2)}</span>
        </div>

        <div className="summary-row total">
          <span>Total</span>
          <span>₹{(cart.total + Math.max(0, cart.subtotal - cart.discountAmount) * 0.18).toFixed(2)}</span>
        </div>

        <div className="coupon-section">
          <div className="coupon-input-group">
            <input
              className="coupon-input"
              placeholder="Enter coupon code"
              value={couponCode}
              onChange={e => setCouponCode(e.target.value.toUpperCase())}
              disabled={applyingCoupon}
            />
            <button
              className="coupon-apply-btn"
              onClick={handleApplyCoupon}
              disabled={applyingCoupon || !couponCode.trim()}
            >
              {applyingCoupon ? '...' : 'Apply'}
            </button>
          </div>
          {couponMsg && (
            <div className={`coupon-msg ${couponMsg.type}`}>{couponMsg.text}</div>
          )}
          <div className="coupon-hint">
            Try: FLAT50 (₹50 off on ₹500+) or SAVE10 (10% off on ₹1000+, max ₹200)
          </div>
        </div>

        {error && <div className="error-msg">{error}</div>}

        <button
          className="checkout-btn"
          onClick={handleCheckout}
          disabled={checkingOut}
        >
          {checkingOut ? 'Processing...' : 'Place Order'}
        </button>
      </div>
    </div>
  );
}
