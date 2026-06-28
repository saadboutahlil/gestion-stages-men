export default async function handler(req, res) {
  const targetHost = 'http://saad777-001-site1.htempurl.com';
  
  // L'URL demandée (ex: /api/auth/login)
  const targetUrl = targetHost + req.url;
  
  try {
    // Cloner les headers du client, mais forcer le Host cible
    const headers = { ...req.headers };
    headers['host'] = 'saad777-001-site1.htempurl.com';
    delete headers['connection'];
    delete headers['content-length'];
    delete headers['x-forwarded-host'];
    delete headers['x-forwarded-proto'];
    delete headers['x-forwarded-for'];
    
    const fetchOptions = {
      method: req.method,
      headers: headers,
    };
    
    // Si ce n'est pas un GET, on reconstruit le body (Vercel le parse automatiquement)
    if (req.method !== 'GET' && req.method !== 'HEAD' && req.body) {
      fetchOptions.body = typeof req.body === 'string' ? req.body : JSON.stringify(req.body);
    }
    
    // Faire la requête vers le HTTP non sécurisé depuis les serveurs Vercel
    const response = await fetch(targetUrl, fetchOptions);
    
    // Lire la réponse
    const data = await response.text();
    const contentType = response.headers.get('content-type');
    
    if (contentType) {
      res.setHeader('Content-Type', contentType);
    }
    
    // Retourner exactement la même chose au navigateur
    res.status(response.status).send(data);
  } catch (err) {
    res.status(500).json({ error: 'Proxy Interne Error', message: err.message });
  }
}
