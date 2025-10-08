import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  vus: 20,
  duration: '1m',
  thresholds: {
    http_req_duration: ['p(95)<300', 'p(99)<600'],
    http_req_failed: ['rate<0.001'],
  },
};

export default function () {
  const res = http.get('http://localhost:5016/api/commerce/pricing/sample');
  check(res, {
    'status is 200': (r) => r.status === 200,
  });
  sleep(0.3);
}