from pyodide.http import pyfetch
import json
import asyncio
from js import document, window
from datetime import datetime

last_seen_time = None

send_message = document.getElementById("send_message")
sender = document.getElementById("sender")
message_text = document.getElementById("message_text")
chat_window = document.getElementById("chat_window")
participants_list = document.getElementById("participants_list")

def append_message(message):
    item = document.createElement("li")
    item.className = "list-group-item"
    item.innerHTML = f'[<b>{message["sender"]}</b>]: <span>{message["text"]}</span><span class="badge text-bg-light text-secondary">{message["time"]}</span>'
    if message["sender"] == sender.value:
        delete_button = document.createElement("button")
        delete_button.className = "btn btn-danger btn-sm"
        delete_button.innerHTML = "Удалить"
        delete_button.onclick = lambda e: asyncio.ensure_future(delete_message_click(message["msg_id"], item))
        item.appendChild(delete_button)
    chat_window.prepend(item)

def update_participants(participants):
    participants_list.innerHTML = ""
    for participant in participants:
        item = document.createElement("li")
        item.className = "list-group-item"
        item.innerHTML = participant
        participants_list.appendChild(item)

async def send_message_click(e):
    if sender.value and message_text.value:
        await fetch(f"/send_message?sender={sender.value}&text={message_text.value}", method="GET")
        message_text.value = ""
    else:
        window.alert("Пожалуйста, введите ваше сообщение")

async def load_fresh_messages():
    global last_seen_time
    if last_seen_time is None:
        last_seen_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    result = await fetch(f"/get_messages?after={last_seen_time}", method="GET")
    data = await result.json()
    if "messages" in data:
        all_messages = data["messages"]
        for msg in all_messages:
            append_message(msg)
            last_seen_time = msg["time"]  # Обновляем last_seen_time для каждого нового сообщения
    set_timeout(1, load_fresh_messages)

async def load_participants():
    result = await fetch("/get_participants", method="GET")
    data = await result.json()
    update_participants(data["participants"])
    set_timeout(5, load_participants)

async def delete_message_click(msg_id, item):
    await fetch("/delete_message", method="POST", payload={"msg_id": msg_id, "sender": sender.value})
    # Удалить элемент сообщения из интерфейса
    chat_window.removeChild(item)

async def delete_other_message():
    result = await fetch(f'/get_deleted_messages', method="GET")
    data = await result.json()
    delete_senders = data['delete_senders']
    for el in chat_window.children:
        sender_name = el.querySelector("b").textContent
        if sender_name in delete_senders:
            chat_window.removeChild(el)
    set_timeout(1, delete_other_message)


async def fetch(url, method, payload=None):
    kwargs = {
        "method": method
    }
    if method == "POST":
        kwargs["body"] = json.dumps(payload)
        kwargs["headers"] = {"Content-Type": "application/json"}
    return await pyfetch(url, **kwargs)

def set_timeout(delay, callback):
    def sync():
        asyncio.get_running_loop().run_until_complete(callback())

    asyncio.get_running_loop().call_later(delay, sync)

# Устанавливаем действие при клике
send_message.onclick = lambda e: asyncio.ensure_future(send_message_click(e))

# Запускаем загрузку новых сообщений
asyncio.ensure_future(load_fresh_messages())

# Запускаем загрузку участников чата
asyncio.ensure_future(load_participants())

# Запускаем удаление сообщений других пользователей
asyncio.ensure_future(delete_other_message())
