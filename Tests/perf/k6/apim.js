import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 10,
  duration: '1m',
  thresholds: {
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.001'],
  },
};

export default function () {
  const payload = JSON.stringify({ service: 'Pricing', endpoint: '/quote', context: {} });
  const params = { headers: { 'Content-Type': 'application/json' } };
  const res = http.post('http://localhost:5016/apim/call', payload, params);
  check(res, { 'status ok': (r) => r.status >= 200 && r.status < 500 });
  sleep(0.3);
}