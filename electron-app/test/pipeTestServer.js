const net = require('net');

const PIPE_NAME = 'snaptrudeDynamoPipe';
const PIPE_PATH = '\\\\.\\pipe\\'; // The format is \\.\pipe\<name>

const server = net.createServer();

// server.listen(path.join(PIPE_PATH, process.cwd(), PIPE_NAME));
server.listen(PIPE_PATH + PIPE_NAME);

server.on('data', (data) => {
  console.log(data.toString());
});

server.on('connection', (socket) => {
  console.log('someone connected to server');
  
  const streamId = "503a180868";
  socket.write(streamId);
  server.close();
});

server.on('end', () => {
  console.log('closed server');
});