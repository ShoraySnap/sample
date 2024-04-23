const net = require("net");

const PIPE_NAME = "XYZNamedPipe";
const PIPE_PATH = "\\\\.\\pipe\\"; // The format is \\.\pipe\<name>

const client = net.createConnection(PIPE_PATH + PIPE_NAME, () => {
  console.log("connected to server!");

  client.write("trueServer");
  // utf8 encoding uses 1 byte per character (for US ascii)
  // on dotnet end, we create a buffer of 10 bytes to read this correctly
});

client.on("data", (data) => {
  console.log(data.toString());
});

client.on("end", () => {
  console.log("disconnected from server");
});
