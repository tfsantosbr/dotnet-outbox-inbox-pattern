import http from 'k6/http';
import { check } from 'k6';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '10m', target: 50 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5176';

export default function () {
  const payload = JSON.stringify({
    customerId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    totalAmount: Math.floor(Math.random() * (9999 - 50 + 1)) + 50,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'correlation-id': uuidv4(),
    },
  };

  const response = http.post(`${BASE_URL}/orders`, payload, params);

  check(response, {
    'status is 2xx': (r) => r.status >= 200 && r.status < 300,
  });
}
