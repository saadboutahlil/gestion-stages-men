const http = require('http');

const loginData = JSON.stringify({
  email: 'admin@men.gov.ma',
  password: 'Admin@2026!'
});

const reqOptions = {
  hostname: 'localhost',
  port: 5014,
  path: '/api/auth/login',
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Content-Length': loginData.length
  }
};

const req = http.request(reqOptions, (res) => {
  let body = '';
  res.on('data', (chunk) => body += chunk);
  res.on('end', () => {
    if (res.statusCode !== 200) {
      console.error('Login failed:', res.statusCode, body);
      process.exit(1);
    }
    const token = JSON.parse(body).token;
    queryArchived(token);
  });
});

req.on('error', (e) => {
  console.error('Problem with login request:', e.message);
});

req.write(loginData);
req.end();

function queryArchived(token) {
  const options = {
    hostname: 'localhost',
    port: 5014,
    path: '/api/internships/archived',
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  };

  const req = http.request(options, (res) => {
    let body = '';
    res.on('data', (chunk) => body += chunk);
    res.on('end', () => {
      if (res.statusCode !== 200) {
        console.error('Request failed:', res.statusCode, body);
        process.exit(1);
      }
      const data = JSON.parse(body);
      console.log('Total archived:', data.length);
      console.log('Sample of records:');
      console.log(JSON.stringify(data.slice(0, 15), null, 2));
    });
  });

  req.on('error', (e) => {
    console.error('Problem with request:', e.message);
  });

  req.end();
}
