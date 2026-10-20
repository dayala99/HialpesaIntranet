import { Injectable } from '@angular/core';
import { OrderDetail } from '../../core/models';

@Injectable({ providedIn: 'root' })
export class SupplierDemoService {
  open(detail: OrderDetail, preparedWindow?: Window): Promise<number> {
    // Open synchronously from the user's click; no database call or saved browser storage.
    const token = crypto.randomUUID();
    const origin = location.origin;
    const url = new URL('prueba-proveedor.html', document.baseURI);
    url.hash = token;
    const payload = {
      version: 1,
      order: { series: detail.order.series, number: detail.order.number, supplier: detail.order.supplier },
      items: detail.items.map(item => ({ sequence: item.sequence, itemCode: item.itemCode, description: item.description, style: item.style, size: item.size, required: item.required, purchased: item.purchased }))
    };
    return new Promise<number>((resolve, reject) => {
      let child: Window | null = null;
      let sent = false;
      let timer: ReturnType<typeof setTimeout>;
      const cleanup = () => { clearTimeout(timer); window.removeEventListener('message', receive); };
      const receive = (event: MessageEvent) => {
        if (event.origin !== origin || event.source !== child || event.data?.token !== token) return;
        if (event.data.type === 'intranet-supplier-ready' && !sent) {
          sent = true; child!.postMessage({ type: 'intranet-supplier-data', token, payload }, origin);
        }
        if (sent && event.data.type === 'intranet-supplier-filled' && Number.isInteger(event.data.count) && event.data.count >= 0 && event.data.count <= payload.items.length) {
          cleanup(); resolve(event.data.count);
        }
      };
      window.addEventListener('message', receive);
      timer = setTimeout(() => { cleanup(); reject(new Error('La página de prueba no confirmó el llenado. Revisa la pestaña abierta o vuelve a intentarlo.')); }, 45000);
      if(preparedWindow){child=preparedWindow;if(child.closed){cleanup();reject(new Error('La pestaña de prueba fue cerrada.'));return;}child.location.href=url.href;}else child = window.open(url.href, '_blank');
      if (!child) { cleanup(); reject(new Error('El navegador bloqueó la pestaña de prueba. Permite las ventanas emergentes para esta intranet.')); }
    });
  }
}
