from flask import Flask, request, jsonify, send_file, send_from_directory
import os
import requests
import json
from datetime import datetime
import time
from urllib.request import urlretrieve
import cv2
import numpy as np
from io import BytesIO
import qrcode
import socket

app = Flask(__name__)

UPLOAD_FOLDER = 'received_images'
ENHANCED_FOLDER = 'enhanced_images'
QR_FOLDER = 'qr_codes'
API_KEY = "3cb74da0-5bcb-11f0-a84d-45e2349af7bf"

os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(ENHANCED_FOLDER, exist_ok=True)
os.makedirs(QR_FOLDER, exist_ok=True)

# ----------------------------
#  CROP FACE ENDPOINT
# ----------------------------
face_cascade = cv2.CascadeClassifier(cv2.data.haarcascades + "haarcascade_frontalface_default.xml")

@app.route('/crop_face', methods=['POST'])
def crop_face():
    if 'image' not in request.files:
        return jsonify({'status': 'error', 'message': 'No image provided'}), 400

    image_file = request.files['image']
    img_bytes = np.frombuffer(image_file.read(), np.uint8)
    img = cv2.imdecode(img_bytes, cv2.IMREAD_COLOR)

    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    faces = face_cascade.detectMultiScale(gray, 1.3, 5)

    if len(faces) == 0:
        return jsonify({'status': 'error', 'message': 'No face detected'}), 400

    x, y, w, h = faces[0]
    face_img = img[y:y+h, x:x+w]

    _, buffer = cv2.imencode('.png', face_img)
    return send_file(BytesIO(buffer.tobytes()), mimetype='image/png')

# ----------------------------
#  ENHANCEMENT ENDPOINT
# ----------------------------
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
        qr_path = generate_qr_code_for_image(enhanced_filepath)

        return jsonify({
            'status': 'success',
            'message': 'Image processed',
            'original_filename': filename,
            'enhanced_filename': os.path.basename(enhanced_filepath),
            'qr_code_filename': os.path.basename(qr_path)
        })

    except Exception as e:
        return jsonify({'status': 'error', 'message': str(e)}), 500

# ----------------------------
# PROCESS WITH AI
# ----------------------------
def process_with_deep_image_ai(image_path, timestamp, gender):
    headers = {'x-api-key': API_KEY}
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
        print(f"Poll {attempt + 1}: {poll_json.get('status')}")
        if poll_json.get('status') == 'complete':
            return download_result_image(poll_json['result_url'], timestamp)

    raise Exception("Processing timed out.")

# ----------------------------
#  DOWNLOAD FINAL IMAGE
# ----------------------------
def download_result_image(url, timestamp):
    enhanced_filename = f"superhero_avatar_{timestamp}.png"
    enhanced_filepath = os.path.join(ENHANCED_FOLDER, enhanced_filename)
    urlretrieve(url, enhanced_filepath)
    print(f"Downloaded enhanced image: {enhanced_filepath}")
    return enhanced_filepath

# ----------------------------
#  GENERATE QR CODE
# ----------------------------
def get_local_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        # Doesn't have to be reachable
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
    finally:
        s.close()
    return ip
def generate_qr_code_for_image(image_path):
    filename = os.path.basename(image_path)
    print("Generating QR code for:", filename)

    local_ip = get_local_ip()
    print("Local IP:", local_ip)

    image_url = f"http://{local_ip}:5000/enhanced_images/{filename}"
    print("Image URL for QR:", image_url)

    qr = qrcode.make(image_url)

    qr_filename = f"qr_{filename.replace('.png', '.png')}"
    qr_path = os.path.join(QR_FOLDER, qr_filename)
    print("Saving QR code to:", qr_path)

    qr.save(qr_path)
    print(f"QR code saved at: {qr_path}")
    return qr_path


# ----------------------------
#  PROMPT PER GENDER
# ----------------------------
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

# ----------------------------
# Serve and List Images
# ----------------------------
@app.route('/enhanced_images/<filename>')
def serve_enhanced_image(filename):
    return send_from_directory(ENHANCED_FOLDER, filename)

@app.route('/enhanced_images/list')
def list_enhanced_images():
    return jsonify([f for f in os.listdir(ENHANCED_FOLDER) if f.endswith('.png')])

@app.route('/qr_codes/<filename>')
def serve_qr_code(filename):
    return send_from_directory(QR_FOLDER, filename)


if __name__ == '__main__':
    print("Flask server running at http://0.0.0.0:5000")
    app.run(host='0.0.0.0', port=5000, debug=True)
