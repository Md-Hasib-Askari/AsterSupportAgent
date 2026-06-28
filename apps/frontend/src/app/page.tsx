import Chat from "./components/Chat";

export default function Home() {
  return (
    <div className="flex flex-col flex-1 min-h-screen">
      <header className="px-6 pt-7 pb-5 border-b border-line text-center">
        <div className="font-serif text-[28px] font-semibold tracking-tight">Aster</div>
        <div className="text-[13px] text-muted mt-1 tracking-wide">
          Support - KB search · order lookup · live booking
        </div>
      </header>
      <Chat />
    </div>
  );
}
