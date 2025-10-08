import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 20,
  duration: '2m',
  thresholds: {
    http_req_duration: ['p(95)<600'],
    http_req_failed: ['rate<0.001'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const res1 = http.get(`${BASE_URL}/api/wallet/balance?userId=user-1`);
  check(res1, { 'wallet balance 200': (r) => r.status === 200 });
  const payload = JSON.stringify({ userId: 'user-1', amount: 10, currency: 'USD' });
  const params = { headers: { 'Content-Type': 'application/json' } };
  const res2 = http.post(`${BASE_URL}/api/wallet/withdraw`, payload, params);
  check(res2, { 'wallet withdraw 2xx': (r) => r.status >= 200 && r.status < 300 });
  sleep(1);
}