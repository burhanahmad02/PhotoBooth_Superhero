from flask import Flask, request, jsonify, send_from_directory
import os
import requests
import json
from datetime import datetime
import time
from urllib.request import urlretrieve

app = Flask(__name__)

UPLOAD_FOLDER = 'received_images'
ENHANCED_FOLDER = 'enhanced_images'
API_KEY = "14a2da80-1541-11f0-80e8-69e4165f51fa"

os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(ENHANCED_FOLDER, exist_ok=True)

@app.route('/upload', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return jsonify({'status': 'error', 'message': 'No image file in request'}), 400
    
    image_file = request.files['image']
    gender = request.form.get('gender', '').lower()

    if gender not in ['man', 'woman']:
        return jsonify({'status': 'error', 'message': 'Invalid or missing gender'}), 400

    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    filename = f"unity_webcam_{timestamp}.png"
    filepath = os.path.join(UPLOAD_FOLDER, filename)

    image_file.save(filepath)
    print(f"Image saved: {filepath}, Gender: {gender}")

    try:
        enhanced_filepath = process_with_deep_image_ai(filepath, timestamp, gender)

        return jsonify({
            'status': 'success',
            'message': 'Image processed',
            'original_filename': filename,
            'enhanced_filename': os.path.basename(enhanced_filepath)
        })

    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500

def process_with_deep_image_ai(image_path, timestamp, gender):
    headers = {
        'x-api-key': API_KEY,
    }

    prompt = get_gender_prompt(gender)

    with open(image_path, 'rb') as f:
        response = requests.post(
            'https://deep-image.ai/rest_api/process_result',
            headers=headers,
            files={'image': f},
            data={
                'parameters': json.dumps({
                    "width": 1024,
                    "height": 1024,
                    "background": {
                        "generate": {
                            "description": prompt,
                            "adapter_type": "face",
                            "face_id": True
                        }
                    }
                })
            }
        )

    response_json = response.json()
    print(f"Initial response: {response_json}")

    if response_json.get('status') == 'complete':
        return download_result_image(response_json['result_url'], timestamp)

    job_id = response_json.get('job')
    for attempt in range(30):
        time.sleep(5)
        poll_response = requests.get(
            f'https://deep-image.ai/rest_api/result/{job_id}',
            headers=headers
        )
        poll_json = poll_response.json()
        print(f"Poll {attempt+1}: {poll_json.get('status')}")
        if poll_json.get('status') == 'complete':
            return download_result_image(poll_json['result_url'], timestamp)

    raise Exception("Processing timed out.")

def download_result_image(url, timestamp):
    enhanced_filename = f"superhero_avatar_{timestamp}.png"
    enhanced_filepath = os.path.join(ENHANCED_FOLDER, enhanced_filename)
    urlretrieve(url, enhanced_filepath)
    print(f"Downloaded enhanced image: {enhanced_filepath}")
    return enhanced_filepath

def get_gender_prompt(gender):
    if gender == 'woman':
        return (
            "A fearless female warrior in ornate armor standing in the desert, sand swirling around her boots. "
            "Sword in hand, confident stance, dramatic desert backdrop, cinematic lighting, full body shot."
        )
    else:
        return (
            "A heroic warrior wearing bronze and black medieval armor, standing confidently in the middle of a vast desert "
            "with sand splashing around and beneath his feet. The sky is clear and blue. A sword is in his hand and another "
            "is sheathed on his back. Cinematic lighting, full body shot."
        )


@app.route('/enhanced_images/<filename>')
def serve_enhanced_image(filename):
    return send_from_directory(ENHANCED_FOLDER, filename)

@app.route('/enhanced_images/list')
def list_enhanced_images():
    return jsonify([f for f in os.listdir(ENHANCED_FOLDER) if f.endswith('.png')])

if __name__ == '__main__':
    print("Flask server running at http://0.0.0.0:5000")
    app.run(host='0.0.0.0', port=5000, debug=True)
