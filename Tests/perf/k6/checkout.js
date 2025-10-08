import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 10,
  duration: '1m',
  thresholds: {
    http_req_duration: ['p(95)<800', 'p(99)<1200'],
    http_req_failed: ['rate<0.001'],
  },
};

export default function () {
  const payload = JSON.stringify({ productId: 'demo', quantity: 1 });
  const params = { headers: { 'Content-Type': 'application/json' } };
  const res = http.post('http://localhost:5016/api/commerce/checkout', payload, params);
  check(res, {
    'status is 2xx/3xx': (r) => r.status >= 200 && r.status < 400,
  });
  sleep(0.5);
}