import os
import json

app_data_store = os.path.join(os.getenv('APPDATA'), 'snaptrude-manager', 'config.json')
file = None

# print(app_data_store)

try:
    file = open(app_data_store)
except Exception as e:
    print('File not found')
    print(e)
    # OUT = ""
    quit()

data = json.load(file)
stream_id = data['streamId']

file.close()

speckle_url = 'https://speckle.xyz/streams/' + stream_id

print(speckle_url)