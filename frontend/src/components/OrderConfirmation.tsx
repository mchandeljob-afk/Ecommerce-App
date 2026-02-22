import { Order } from '../types';

interface Props {
  order: Order;
  onContinueShopping: () => void;
}

export default function OrderConfirmation({ order, onContinueShopping }: Props) {
  return (
    <div className="order-confirmation">
      <div className="success-icon">✅</div>
      <h2>Order Confirmed!</h2>
      <div className="order-id">Order ID: {order.orderId}</div>

      <div className="order-items">
        <table>
          <thead>
            <tr>
              <th>Product</th>
              <th>Qty</th>
              <th>Unit Price</th>
              <th>Total</th>
            </tr>
          </thead>
          <tbody>
            {order.items.map((item, idx) => (
              <tr key={idx}>
                <td>{item.productName}</td>
                <td>{item.quantity}</td>
                <td>₹{item.unitPrice.toFixed(2)}</td>
                <td>₹{item.lineTotal.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="pricing-breakdown">
        <div className="summary-row">
          <span>Subtotal</span>
          <span>₹{order.subtotal.toFixed(2)}</span>
        </div>
        {order.discount > 0 && (
          <div className="summary-row discount">
            <span>Discount {order.couponCode && `(${order.couponCode})`}</span>
            <span>-₹{order.discount.toFixed(2)}</span>
          </div>
        )}
        <div className="summary-row">
          <span>Tax (18% GST)</span>
          <span>₹{order.tax.toFixed(2)}</span>
        </div>
        <div className="summary-row total">
          <span>Grand Total</span>
          <span>₹{order.grandTotal.toFixed(2)}</span>
        </div>
      </div>

      <button className="continue-btn" onClick={onContinueShopping}>
        Continue Shopping
      </button>
    </div>
  );
}
