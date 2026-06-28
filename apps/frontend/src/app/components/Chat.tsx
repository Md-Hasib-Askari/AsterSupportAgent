'use client';

import React, { useEffect, useRef, useState } from 'react';

interface TraceStep {
  type: string;
  action?: {
    action: string;
  };
}

interface ChatResponse {
  reply: string;
  trace?: TraceStep[];
  error?: string;
}

const TOOL_LABELS: Record<string, string> = {
  search_kb: 'searched help docs',
  get_order_status: 'looked up order',
  create_booking_link: 'created Calendly booking link',
};

export default function Chat() {

  const [messages, setMessages] = useState<{ role: 'user' | 'bot'; text: string; trace?: TraceStep[] }[]>([
    { role: 'bot', text: "Hi, I'm Aster's support agent. I can check order status, answer shipping/return questions, or get you a real booking link to talk to a human." },
  ]);
  const [inputText, setInputText] = useState('');
  const [typing, setTyping] = useState(false);
  const threadRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const sessionId = useRef<string>('sess-' + Math.random().toString(36).slice(2));

  useEffect(() => {
    const el = threadRef.current;
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }, [messages, typing]);

  const apiBase = process.env.NEXT_PUBLIC_API_URL || '';

  useEffect(() => {
    console.log('Chat mounted — API Base URL:', apiBase);
  }, []);

  async function handleSend(text: string) {
    setMessages((prev) => [...prev, { role: 'user', text }]);
    setTyping(true);
    setInputText('');

    try {
      const res = await fetch(`${apiBase}/api/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: text, sessionId: sessionId.current }),
      });
      const data: ChatResponse = await res.json();
      setTyping(false);

      if (data.error) {
        setMessages((prev) => [...prev, { role: 'bot', text: 'Sorry — something went wrong: ' + data.error }]);
      } else {
        setMessages((prev) => [...prev, { role: 'bot', text: data.reply, trace: data.trace }]);
      }
    } catch {
      setTyping(false);
      setMessages((prev) => [...prev, { role: 'bot', text: "Couldn't reach the server. Is it running?" }]);
    }
  }

  function sendMessage() {
    const text = inputText.trim();
    if (!text) return;
    handleSend(text);
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') {
      e.preventDefault();
      sendMessage();
    }
  }

  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {
    e.preventDefault();
    sendMessage();
  }

  return (
    <>
      <main ref={threadRef} className="flex-1 max-w-[640px] w-full mx-auto px-4 pb-36 flex flex-col gap-4 overflow-y-auto">
        {messages.map((msg, i) => (
          <div
            key={i}
            className={`msg flex flex-col max-w-[86%] animate-[rise_0.25s_ease_both] ${
              msg.role === 'user' ? 'self-end items-end' : 'self-start items-start'
            }`}
          >
            <div
              className={`bubble px-4 py-3 rounded-[14px] text-[15px] leading-relaxed ${
                msg.role === 'user'
                  ? 'bg-ink text-paper rounded-br-[4px]'
                  : 'bg-paper-raised border border-line rounded-bl-[4px]'
              }`}
            >
              {msg.text}
            </div>
            {msg.trace && msg.trace.length > 0 && (
              <div className="receipt mt-1.5 flex flex-col gap-0.5 pl-0.5">
                {msg.trace
                  .filter((t) => t.type === 'tool_call')
                  .map((tc, idx) => (
                    <div key={idx} className="receipt-line text-[11px] text-muted flex items-center gap-1.5">
                      <span className="w-[5px] h-[5px] rounded-full bg-thread flex-shrink-0" />
                      <span className="font-semibold text-thread">
                        {TOOL_LABELS[tc.action?.action ?? ''] || tc.action?.action}
                      </span>
                    </div>
                  ))}
              </div>
            )}
          </div>
        ))}
        {typing && (
          <div className="msg flex flex-col max-w-[86%] self-start items-start">
            <div className="bubble typing px-4 py-3.5 rounded-[14px] bg-paper-raised border border-line rounded-bl-[4px]">
              <div className="flex gap-1">
                <span className="w-1.5 h-1.5 rounded-full bg-muted animate-[bounce_1.2s_infinite_ease-in-out]" />
                <span className="w-1.5 h-1.5 rounded-full bg-muted animate-[bounce_1.2s_infinite_ease-in-out] [animation-delay:0.15s]" />
                <span className="w-1.5 h-1.5 rounded-full bg-muted animate-[bounce_1.2s_infinite_ease-in-out] [animation-delay:0.3s]" />
              </div>
            </div>
          </div>
        )}
      </main>

      <div className="composer fixed bottom-0 left-0 right-0 bg-linear-to-t from-paper from-60% to-transparent px-4 py-5">
        <div className="max-w-[640px] mx-auto flex gap-2 bg-paper-raised border border-line rounded-2xl px-4 py-2 shadow-md">
          <input
            ref={inputRef}
            type="text"
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask about an order, shipping, returns…"
            autoComplete="off"
            className="flex-1 bg-transparent outline-none text-[15px] text-ink placeholder:text-muted focus-visible:outline-2 focus-visible:outline-thread focus-visible:outline-offset-2"
          />
          <button
            type="button"
            onClick={handleClick}
            aria-label="Send"
            className="w-[38px] h-[38px] bg-ink text-paper rounded-[11px] flex items-center justify-center flex-shrink-0 transition-colors hover:bg-thread focus-visible:outline-2 focus-visible:outline-thread focus-visible:outline-offset-2"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path
                d="M1 8L15 1L9 15L7 9L1 8Z"
                stroke="currentColor"
                strokeWidth="1.4"
                strokeLinejoin="round"
              />
            </svg>
          </button>
        </div>
      </div>
    </>
  );
}
