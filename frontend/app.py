"""
ChatApp Frontend - Gradio UI
Complete login + chat interface with OTP authentication.

Learning topics:
1. Auth flow: Phone -> OTP -> Token
2. State management: gr.State for session persistence
3. API integration: REST calls to .NET backend
"""

import gradio as gr
import os
import requests

# ============================================================================
# CONFIGURATION
# ============================================================================
API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5289")

# ============================================================================
# AUTH FUNCTIONS
# ============================================================================

def send_otp(phone_number):
    """Request OTP code for phone number."""
    try:
        response = requests.post(
            f"{API_BASE_URL}/api/auth/send-otp",
            json={"phoneNumber": phone_number},
            timeout=5
        )
        if response.status_code == 200:
            return "OTP sent! Demo mode is on, so any OTP value will work."
        return f"Error: {response.json().get('error', 'Unknown error')}"
    except Exception as e:
        return f"API connection failed: {str(e)}"


def verify_otp(phone_number, otp_code):
    """Verify OTP and get auth token."""
    try:
        response = requests.post(
            f"{API_BASE_URL}/api/auth/verify-otp",
            json={"phoneNumber": phone_number, "code": otp_code},
            timeout=5
        )
        if response.status_code == 200:
            data = response.json()
            token = data.get('token')
            user = data.get('user')
            if token:
                return token, user, f"✅ Logged in as {user.get('displayName', phone_number)}"
            return None, None, "❌ Login response missing token"
        return None, None, response.json().get('error', 'Invalid OTP')
    except Exception as e:
        return None, None, f'API connection failed: {str(e)}'


def create_conversation(token, phone_number):
    """Create a new conversation with another user."""
    try:
        response = requests.post(
            f'{API_BASE_URL}/api/conversations',
            json={'phoneNumber': phone_number},
            headers={'Authorization': f'Bearer {token}'},
            timeout=5
        )
        if response.status_code == 200 or response.status_code == 201:
            return f'Conversation created with {phone_number}'
        return f"Error: {response.json().get('error', 'Unknown error')}"
    except Exception as e:
        return f'API connection failed: {str(e)}'


def get_conversations(token):
    """Fetch all conversations for the current user."""
    try:
        response = requests.get(
            f'{API_BASE_URL}/api/conversations',
            headers={'Authorization': f'Bearer {token}'},
            timeout=5
        )
        if response.status_code == 200:
            return response.json()
        return []
    except:
        return []


def send_message(token, conversation_id, content):
    """Send a message to a conversation."""
    try:
        response = requests.post(
            f'{API_BASE_URL}/api/messages',
            json={'conversationId': conversation_id, 'content': content},
            headers={'Authorization': f'Bearer {token}'},
            timeout=5
        )
        if response.status_code == 201:
            return 'Message sent!'
        return f'Error: {response.status_code}'
    except Exception as e:
        return f'API connection failed: {str(e)}'


# ============================================================================
# GRADIO UI
# ============================================================================

def build_ui():
    """Build the complete Gradio app."""
    
    with gr.Blocks(title='ChatApp', theme=gr.themes.Soft()) as demo:
        gr.Markdown('# ChatApp - OTP Auth Demo')
        gr.Markdown('**Auth flow:** Phone Number -> OTP -> Token')
        
        token_state = gr.State(value=None)
        user_state = gr.State(value=None)
        
        with gr.TabItem('Login', id='login_tab') as login_tab:
            gr.Markdown('### Login to ChatApp')
            phone_input = gr.Textbox(label='Phone Number', placeholder='+1234567890')
            send_otp_btn = gr.Button('Get OTP', variant='primary')
            otp_result = gr.Textbox(label='Status')
            otp_input = gr.Textbox(label='OTP Code', placeholder='Enter OTP')
            verify_btn = gr.Button('Verify', variant='secondary')
            auth_result = gr.Textbox(label='Auth Result')
        
        with gr.TabItem('Chat', id='chat_tab') as chat_tab:
            chat_tab.visible = False
            gr.Markdown('### Start Chatting')
            
            with gr.Row():
                with gr.Column(scale=1):
                    conversations = gr.Dropdown(label='Conversations', choices=[], value=None)
                    refresh_btn = gr.Button('Refresh')
                    logout_btn = gr.Button('Logout', variant='stop')
                
                with gr.Column(scale=3):
                    chat_history = gr.Textbox(label='Chat History', lines=20, value='Select a conversation...')
                    
                    with gr.Accordion('Create New Conversation', open=False):
                        new_phone = gr.Textbox(label='Contact Phone', placeholder='+1234567890')
                        create_conv_btn = gr.Button('Create Conversation')
                        conv_result = gr.Textbox(label='Result')
                    
                    message_input = gr.Textbox(label='Your Message', placeholder='Type here...')
                    send_btn = gr.Button('Send', variant='primary')
        
        # Event handlers
        send_otp_btn.click(fn=send_otp, inputs=phone_input, outputs=otp_result)
        
        verify_btn.click(
            fn=verify_otp,
            inputs=[phone_input, otp_input],
            outputs=[token_state, user_state, auth_result]
        ).then(
            fn=lambda token: (gr.update(visible=False), gr.update(visible=True)) if token else (gr.update(visible=True), gr.update(visible=False)),
            inputs=token_state,
            outputs=[login_tab, chat_tab]
        ).then(
            fn=lambda token: [c.get('id') for c in get_conversations(token)] if token else [],
            inputs=token_state,
            outputs=conversations
        )
        
        logout_btn.click(
            fn=lambda: (gr.update(visible=True), gr.update(visible=False), None, None),
            outputs=[login_tab, chat_tab, token_state, user_state]
        )
        
        refresh_btn.click(
            fn=lambda token: get_conversations(token) if token else [],
            inputs=token_state,
            outputs=conversations
        )
        
        create_conv_btn.click(
            fn=lambda token, phone: create_conversation(token, phone) if token else 'Please login first',
            inputs=[token_state, new_phone],
            outputs=conv_result
        )
        
        send_btn.click(
            fn=lambda token, conv_id, msg: send_message(token, conv_id, msg) if token else 'Please login first',
            inputs=[token_state, conversations, message_input],
            outputs=chat_history
        ).then(
            fn=lambda msg: '',
            inputs=message_input,
            outputs=message_input
        )
    
    return demo


if __name__ == '__main__':
    print('Starting ChatApp Gradio Frontend...')
    print(f'API URL: {API_BASE_URL}')
    print('Auth flow: Phone -> OTP -> Token')
    demo = build_ui()
    demo.launch(share=False)
