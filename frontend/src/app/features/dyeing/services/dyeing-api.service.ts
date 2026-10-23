import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { DyeingFilter, DyeingPage, DyeingCatalogOption } from '../models/dyeing.models';

@Injectable({ providedIn: 'root' })
export class DyeingApiService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl + '/tintoreria/partidas';
  catalogs() { return this.http.get<DyeingCatalogOption[]>(this.base + '/catalogos', { withCredentials: true }); }
  search(filter: DyeingFilter) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) if (value !== '') params = params.set(key, value);
    return this.http.get<DyeingPage>(this.base, { params, withCredentials: true });
  }
}
