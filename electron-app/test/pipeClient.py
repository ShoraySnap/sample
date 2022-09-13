pipe_id = '\\\\?\\pipe\\'

# in ironpython \\?\ gives invalid character error
# and \\.\ gives ValueError: FileStream will not open Win32 devices
# because of this https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file?redirectedfrom=MSDN#win32-device-namespaces
# so works in py but not iron python

pipe_name = 'snaptrudeDynamoPipe'

pipein = open(pipe_id + pipe_name, 'r')
stream_id = pipein.read(10) # 10 alpha numeric characters

speckle_url = 'https://speckle.xyz/streams/' + stream_id

print(speckle_url)

# OUT = stream_id