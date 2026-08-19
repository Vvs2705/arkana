package br.com.vstack.arkana;

import android.os.Bundle;

import androidx.core.view.WindowCompat;
import androidx.core.view.WindowInsetsCompat;
import androidx.core.view.WindowInsetsControllerCompat;

import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        hideSystemBars();
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        // Diálogos/troca de app devolvem as barras — re-esconde ao voltar o foco.
        if (hasFocus) hideSystemBars();
    }

    /**
     * Tela cheia imersiva (padrão de jogo landscape): esconde barra de status e
     * de navegação; um swipe da borda as mostra transitoriamente e elas somem
     * de novo sozinhas. `android:windowFullscreen` no tema deixou de bastar com
     * targetSdk 35+ (edge-to-edge obrigatório ignora a flag legada) — por isso
     * o caminho é o WindowInsetsController, sem plugin nenhum.
     */
    private void hideSystemBars() {
        WindowCompat.setDecorFitsSystemWindows(getWindow(), false);
        WindowInsetsControllerCompat controller =
                WindowCompat.getInsetsController(getWindow(), getWindow().getDecorView());
        controller.hide(WindowInsetsCompat.Type.systemBars());
        controller.setSystemBarsBehavior(
                WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE);
    }
}
