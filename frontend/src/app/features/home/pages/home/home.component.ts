import {SessionService} from '../../../../core/auth/session.service';
import {Component,inject} from '@angular/core';
@Component({selector:'app-home',templateUrl:'./home.component.html',styleUrl:'./home.component.scss'})
export class HomeComponent {
 readonly session=inject(SessionService);
 private readonly options=[
  {permission:'orders.query',group:'Logística',title:'Analíticas',path:'/logistica/analiticas',icon:'hub',description:'Consulta la cantidad programada por NP.'},
  {permission:'orders.create',group:'Logística',title:'Nueva orden de compra',path:'/logistica/avios/nueva',icon:'add',description:'Registra una nueva orden de compra.'},
  {permission:'orders.query',group:'Logística',title:'Consulta OC y PO',path:'/logistica/avios',icon:'inventory_2',description:'Consulta órdenes de compra y PO de Gestión.'},
  {permission:'imports',group:'Logística',title:'Importaciones',path:'/logistica/importaciones',icon:'flight_takeoff',description:'Consulta y gestiona las importaciones.'},
  {permission:'dyeing.batches',group:'Tintorería',title:'Generación Partidas - Tintoreria',path:'/tintoreria/generacion-partidas',icon:'palette',description:'Consulta partidas y sus datos de control de calidad.'},
  {permission:'quality.events',group:'Aseguramiento Calidad',title:'Eventos Tintorería',path:'/aseguramiento-calidad/eventos-tintoreria',icon:'event_note',description:'Registra y consulta los eventos de tintorería.'},
  {permission:'quality.unfinished',group:'Aseguramiento Calidad',title:'Eventos Tintorería - Tela Por Acabar',path:'/aseguramiento-calidad/eventos-tintoreria-tela-por-acabar',icon:'fact_check',description:'Registra y consulta los eventos de telas por acabar.'}
 ];
 get available(){return this.options.filter(option=>this.session.can(option.permission));}
 get groups(){return ['Logística','Tintorería','Aseguramiento Calidad'].map(title=>({title,options:this.available.filter(option=>option.group===title)})).filter(group=>group.options.length);}
}
