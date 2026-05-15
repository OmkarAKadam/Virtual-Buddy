from flask import Flask, request, jsonify
from flask_cors import CORS

import pyperclip
import pyautogui
import webbrowser
import urllib.parse
import threading
import subprocess

from plyer import notification

app = Flask(__name__)
CORS(app)

# Prevent pyautogui fail-safe interruptions
pyautogui.FAILSAFE = False


def success_response(result):
    return jsonify({
        "success": True,
        "result": result
    })


def error_response(error_message, status_code=500):
    return jsonify({
        "success": False,
        "error": str(error_message)
    }), status_code


@app.route("/health", methods=["GET"])
def health():
    try:
        return success_response("Virtual Buddy server running")
    except Exception as e:
        return error_response(e)


@app.route("/search", methods=["POST"])
def search():
    try:
        data = request.get_json()

        if not data or "query" not in data:
            return error_response("Missing 'query' field", 400)

        query = data["query"].strip()

        if not query:
            return error_response("Query cannot be empty", 400)

        encoded_query = urllib.parse.quote(query)
        url = f"https://www.bing.com/search?q={encoded_query}"

        # Open in default browser (Edge if set as default)
        threading.Thread(target=subprocess.Popen, args=(["cmd", "/c", "start", "msedge", url],)).start()

        return success_response(f"Search opened for: {query}")

    except Exception as e:
        return error_response(e)


@app.route("/clipboard/read", methods=["POST"])
def clipboard_read():
    try:
        content = pyperclip.paste()

        return success_response(content)

    except Exception as e:
        return error_response(e)


@app.route("/clipboard/write", methods=["POST"])
def clipboard_write():
    try:
        data = request.get_json()

        if not data or "text" not in data:
            return error_response("Missing 'text' field", 400)

        text = str(data["text"])

        pyperclip.copy(text)

        return success_response("Clipboard updated successfully")

    except Exception as e:
        return error_response(e)


@app.route("/type", methods=["POST"])
def type_text():
    try:
        data = request.get_json()

        if not data or "text" not in data:
            return error_response("Missing 'text' field", 400)

        text = str(data["text"])

        # Type asynchronously to avoid blocking
        threading.Thread(target=pyautogui.write, args=(text,), kwargs={"interval": 0.02}).start()

        return success_response("Text typing started")

    except Exception as e:
        return error_response(e)


@app.route("/notify", methods=["POST"])
def notify():
    try:
        data = request.get_json()

        if not data:
            return error_response("Request body missing", 400)

        title = data.get("title", "Virtual Buddy")
        message = data.get("message", "")

        if not message:
            return error_response("Missing 'message' field", 400)

        notification.notify(
            title=title,
            message=message,
            app_name="Virtual Buddy",
            timeout=5
        )

        return success_response("Notification sent")

    except Exception as e:
        return error_response(e)


@app.errorhandler(404)
def not_found(_):
    return error_response("Route not found", 404)


@app.errorhandler(405)
def method_not_allowed(_):
    return error_response("Method not allowed", 405)


if __name__ == "__main__":
    print("Starting Virtual Buddy companion server...")
    app.run(
        host="127.0.0.1",
        port=5000,
        debug=False
    )