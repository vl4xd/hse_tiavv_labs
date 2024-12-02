from flask import Flask, request, render_template, jsonify
from datetime import datetime
import json
import os
#import time

app = Flask(__name__, static_folder="./client", template_folder="./client")

msg_id = 1
all_messages = []
deleted_senders = []
participants = set()

# Загрузка сообщений из файла при запуске
if os.path.exists("messages.json"):
    with open("messages.json", "r") as f:
        all_messages = json.load(f)
        if all_messages:
            msg_id = max(msg["msg_id"] for msg in all_messages) + 1

@app.route("/chat")
def chat_page():
    return render_template("chat.html")

def add_message(sender, text):
    deleted_senders.clear()
    global msg_id
    new_message = {
        "sender": sender,
        "text": text,
        "time": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "msg_id": msg_id
    }
    msg_id += 1
    all_messages.append(new_message)
    save_messages()

def save_messages():
    with open("messages.json", "w") as f:
        json.dump(all_messages, f)

@app.route("/get_messages")
def get_messages():
    after = request.args.get("after", None)
    if after:
        after_time = datetime.strptime(after, "%Y-%m-%d %H:%M:%S")
        filtered_messages = [msg for msg in all_messages if datetime.strptime(msg["time"], "%Y-%m-%d %H:%M:%S") > after_time]
        #filtred_deleted_messages = [msg for msg in filtered_messages if not msg['msg_id'] in deleted_messages]
    else:
        filtered_messages = all_messages
    deleted_senders = []
    return jsonify({"messages": filtered_messages})


@app.route("/send_message")
def send_message():
    sender = request.args.get("sender")
    text = request.args.get("text")
    if sender and text:
        add_message(sender, text)
        participants.add(sender)
        return jsonify({"result": True})
    return jsonify({"result": False, "error": "Missing sender or text"}), 400

@app.route("/get_participants")
def get_participants():
    return jsonify({"participants": list(participants)})

@app.route("/delete_message", methods=["POST"])
def delete_message():
    data = request.json
    msg_id = data.get("msg_id")
    msg_sender = data.get("sender")
    deleted_senders.append(msg_sender)
    sender = data.get("sender")
    global all_messages
    all_messages = [msg for msg in all_messages if not (msg["msg_id"] == msg_id and msg["sender"] == sender)]
    save_messages()
    return jsonify({"result": True})

@app.route("/get_deleted_messages", methods=["GET"])
def get_deleted_messages():
    return jsonify({"delete_senders":deleted_senders})


@app.route("/")
def hello_page():
    return "New text goes here"

if __name__ == "__main__":
    app.run()
