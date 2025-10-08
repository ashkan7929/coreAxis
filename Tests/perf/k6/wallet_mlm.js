import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 10,
  duration: '1m',
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.001'],
  },
};

export default function () {
  const res1 = http.get('http://localhost:5016/api/wallet/accounts');
  check(res1, { 'wallet ok': (r) => r.status === 200 });
  const res2 = http.get('http://localhost:5016/api/mlm/commissions');
  check(res2, { 'mlm ok': (r) => r.status >= 200 && r.status < 500 });
  sleep(0.4);
}