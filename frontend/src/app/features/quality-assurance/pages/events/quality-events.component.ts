import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
@Component({selector:'app-quality-events',standalone:true,templateUrl:'./quality-events.component.html'})
export class QualityEventsComponent {
 readonly title=inject(ActivatedRoute).snapshot.data['heading'] as string;
}
