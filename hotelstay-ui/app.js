// HotelStay UI - framework-free SPA
const API_BASE = 'http://localhost:5000';

const knownCities = ["New York","London","Paris","Tokyo","Sydney"];

const App = {
  state: { view: 'search', search: null, results: [], loading:false, error:null, sortAsc:true, reservation:null },
  init(){
    this.root = document.getElementById('main');
    this.renderSearch();
  },

  // ---------- Helpers ----------
  el(tag, attrs={}, ...children){
    const e = document.createElement(tag);
    for(const k in attrs){
      if(k==='class') e.className = attrs[k];
      else if(k.startsWith('on') && typeof attrs[k]==='function') e.addEventListener(k.slice(2), attrs[k]);
      else if(k==='html') e.innerHTML = attrs[k];
      else e.setAttribute(k, attrs[k]);
    }
    for(const c of children) if(c!=null) e.appendChild(typeof c==='string'?document.createTextNode(c):c);
    return e;
  },

  setView(view){ this.state.view=view; this.root.innerHTML=''; if(view==='search') this.renderSearch();
    else if(view==='results') this.renderResults();
    else if(view==='reserve') this.renderReserve();
    else if(view==='confirm') this.renderConfirm();
  },

  // ---------- Search View ----------
  renderSearch(){
    const card = this.el('div',{class:'card'});
    const form = this.el('form');

    // Destination: choose from knownCities (dropdown only)
    const destLabel = this.el('label',{},'Destination');
    const destinationSelect = this.el('select',{id:'destination'});
    destinationSelect.appendChild(this.el('option',{value:''},'Choose a city...'));
    knownCities.forEach(c=> destinationSelect.appendChild(this.el('option',{value:c},c)));

    // Dates
    const checkInLabel = this.el('label',{},'Check-in');
    const checkIn = this.el('input',{type:'date',id:'checkIn'});
    const checkOutLabel = this.el('label',{},'Check-out');
    const checkOut = this.el('input',{type:'date',id:'checkOut'});

    // Room type
    const roomLabel = this.el('label',{},'Room type (optional)');
    const roomSelect = this.el('select',{id:'roomType'});
    ['Any','Standard','Deluxe','Suite'].forEach(r=>roomSelect.appendChild(this.el('option',{value:r},r)));

    const submit = this.el('button',{class:'btn',type:'submit'},'Search');

    // Replace free-text destination with a single dropdown occupying the same space
    form.appendChild(this.el('div',{class:'form-row'}, this.el('div',{},destLabel, destinationSelect)));
    form.appendChild(this.el('div',{class:'form-row'}, this.el('div',{},checkInLabel, checkIn), this.el('div',{},checkOutLabel, checkOut)));
    form.appendChild(this.el('div',{class:'form-row'}, this.el('div',{},roomLabel, roomSelect), submit));

    const clientError = this.el('div',{class:'inline-error',id:'clientError'});
    card.appendChild(form); card.appendChild(clientError);

    form.addEventListener('submit', async (e)=>{
      e.preventDefault(); clientError.textContent='';
      const dest = destinationSelect.value.trim(); const ci = checkIn.value; const co = checkOut.value; const room = roomSelect.value;
      // Client-side validation matching API 400 rules
      if(!dest || !ci || !co){ clientError.textContent='Destination, check-in and check-out are required.'; return; }
      if(new Date(co) <= new Date(ci)){ clientError.textContent='Check-out must be after check-in.'; return; }
      // All good
      this.state.search = {destination:dest, checkIn:ci, checkOut:co, roomType: room==='Any'?null:room};
      await this.doSearch();
    });

    this.root.appendChild(card);
  },

  // ---------- Search / Results ----------
  async doSearch(){
    this.state.view='results'; this.setView('results');
    this.state.loading=true; this.state.error=null; this.updateResultsState();
    try{
      const q = new URLSearchParams({destination:this.state.search.destination,checkIn:this.state.search.checkIn,checkOut:this.state.search.checkOut});
      if(this.state.search.roomType) q.set('roomType',this.state.search.roomType);
      const res = await fetch(API_BASE + '/hotels/search?' + q.toString());
      if(!res.ok){
        const txt = await res.text(); throw new Error(txt || res.statusText);
      }
      const data = await res.json();
      // API returns { results: [...] } in the scaffold. Accept either form for compatibility.
      this.state.results = Array.isArray(data) ? data : (Array.isArray(data.results) ? data.results : []);
      this.state.loading=false; this.state.error=null; this.updateResultsState();
    }catch(err){
      this.state.loading=false; this.state.error = err.message || 'Unknown error'; this.updateResultsState();
    }
  },

  renderResults(){
    const container = this.el('div',{class:'card'});
    container.appendChild(this.el('div',{}, this.el('strong',{},'Results for: '), `${this.state.search.destination} — ${this.state.search.checkIn} to ${this.state.search.checkOut}`));

    const controls = this.el('div',{style:'margin-top:8px;display:flex;justify-content:space-between;align-items:center'});
    const back = this.el('button',{class:'btn',onclick:()=>this.setView('search')},'New search');
    const sortBtn = this.el('button',{class:'sort-toggle',onclick:()=>{ this.state.sortAsc=!this.state.sortAsc; this.updateResultsState(); }}, 'Sort: Price ⬆');
    controls.appendChild(back); controls.appendChild(sortBtn);
    container.appendChild(controls);

    const stateDiv = this.el('div',{id:'resultsState'});
    container.appendChild(stateDiv);
    this.root.appendChild(container);
    this.updateResultsState();
  },

  updateResultsState(){
    const stateDiv = document.getElementById('resultsState'); if(!stateDiv) return;
    stateDiv.innerHTML='';
    if(this.state.loading){ stateDiv.appendChild(this.el('div',{class:'state center'}, 'Loading...')); return; }
    if(this.state.error){ stateDiv.appendChild(this.el('div',{class:'state error'}, this.state.error)); return; }
    const list = this.state.results.slice();
    const getTotalAmount = (room) => {
      // room.totalPrice may be an object { amount, currency } or a numeric 'total' field
      const v = room?.totalPrice?.amount ?? room?.total?.amount ?? room?.total ?? room?.totalPrice;
      const n = Number(v);
      return Number.isFinite(n) ? n : 0;
    };
    list.sort((a,b)=>{ const pa = getTotalAmount(a); const pb = getTotalAmount(b); return this.state.sortAsc? pa-pb: pb-pa; });
    const resultsDiv = this.el('div',{class:'results'});
    if(list.length===0){ stateDiv.appendChild(this.el('div',{class:'state center muted'}, 'No rooms found')); return; }

    const mapRoomTypeNumber = (n)=>{
      // RoomType enum mapping from server: 0=Standard,1=Deluxe,2=Suite, others -> Unknown
      switch(n){
        case 0: return 'Standard';
        case 1: return 'Deluxe';
        case 2: return 'Suite';
        default: return 'Room';
      }
    };

    list.forEach(room =>{
      const r = this.el('div',{class:'room'});
      const meta = this.el('div',{class:'meta'});
      meta.appendChild(this.el('div',{}, this.el('span',{class:'badge'}, room.provider || 'Provider')));
      const roomTypeLabel = (typeof room.roomType === 'number') ? mapRoomTypeNumber(room.roomType) : (room.roomType || 'Room');
      meta.appendChild(this.el('div',{}, this.el('strong',{}, roomTypeLabel)));
      // cancellationPolicy may be an object { description: ... } or a string
      const cancellationText = room.cancellationPolicy?.description ?? room.cancellationPolicy ?? 'No policy';
      meta.appendChild(this.el('div',{class:'small muted'}, cancellationText));

      const right = this.el('div',{class:'right'});
      // Normalize numeric values from server-shaped payloads
      const perNight = Number(room.ratePerNightDisplay?.amount ?? room.perNightRates?.[0]?.amount ?? room.rate ?? 0) || 0;
      const total = Number(room.totalPrice?.amount ?? room.total ?? 0) || 0;
      const currency = room.ratePerNightDisplay?.currency ?? room.totalPrice?.currency ?? 'USD';
      right.appendChild(this.el('div',{class:'small'}, `Per-night: ${currency} ${perNight.toFixed(2)}`));
      right.appendChild(this.el('div',{class:'small'}, `Total: ${currency} ${total.toFixed(2)}`));
      const book = this.el('button',{class:'btn',onclick:()=>{ this.state.reservation={room,search:this.state.search}; this.setView('reserve'); }},'Reserve');
      right.appendChild(this.el('div',{},book));

      r.appendChild(meta); r.appendChild(right);
      resultsDiv.appendChild(r);
    });

    stateDiv.appendChild(resultsDiv);
    // update sort label
    const sortBtn = document.querySelector('.sort-toggle'); if(sortBtn) sortBtn.textContent = 'Sort: Price ' + (this.state.sortAsc? '⬆':'⬇');
  },

  // ---------- Reservation ----------
  renderReserve(){
    const card = this.el('div',{class:'card'});
    const header = this.el('div',{}, this.el('strong',{}, 'Reserve: '), `${this.state.reservation.room.roomType} — ${this.state.reservation.room.provider}`);
    card.appendChild(header);

    const form = this.el('form');
    const nameLabel = this.el('label',{},'Guest full name');
    const nameInput = this.el('input',{type:'text',id:'guestName'});
    const docLabel = this.el('label',{},'Document type');
    const docSelect = this.el('select',{id:'docType'});
    ['Passport','National ID'].forEach(d=>docSelect.appendChild(this.el('option',{value:d},d)));
    const docNumLabel = this.el('label',{},'Document number');
    const docNum = this.el('input',{type:'text',id:'docNum'});
    const inlineErr = this.el('div',{class:'inline-error',id:'reserveClientError'});

    form.appendChild(this.el('div',{class:'form-grid'}, this.el('div',{},nameLabel,nameInput), this.el('div',{},docLabel,docSelect)));
    form.appendChild(this.el('div',{},docNumLabel,docNum));
    form.appendChild(this.el('div',{}, this.el('button',{class:'btn',type:'submit'},'Confirm reservation'), this.el('button',{type:'button',onclick:()=>this.setView('results')},'Back')));

    card.appendChild(form); card.appendChild(inlineErr);
    this.root.appendChild(card);

    // client-side pre-validate document type against destination
    const checkDocSync = ()=>{
      inlineErr.textContent='';
      const dest = this.state.reservation.search.destination.toLowerCase(); const doc = docSelect.value;
      // Prompt 5.1 rule: assume some destinations require Passport only: e.g., 'United' or 'US' or is not in EU list require Passport? Because original prompt 5.1 isn't here, choose a simple rule: if destination contains 'United' or 'US' or is not in EU list require Passport. Use knownCities mapping.
      const passportOnlyFor = ['Tokyo','Sydney'];
      if(passportOnlyFor.includes(this.state.reservation.search.destination) && doc!=='Passport'){
        inlineErr.textContent = 'For this destination a Passport is required.'; return false;
      }
      return true;
    };

    docSelect.addEventListener('change', checkDocSync);

    form.addEventListener('submit', async (e)=>{
      e.preventDefault(); inlineErr.textContent='';
      const name = nameInput.value.trim(); const docType = docSelect.value; const docNumber = docNum.value.trim();
      if(!name || !docNumber){ inlineErr.textContent='Name and document number are required.'; return; }
      if(!checkDocSync()) return; // client-side inline

      // submit to API
      try{
        const room = this.state.reservation.room;
        const ratePerNight = Number(room.ratePerNightDisplay?.amount ?? room.perNightRates?.[0]?.amount ?? room.rate ?? 0) || 0;
        const currency = room.ratePerNightDisplay?.currency ?? room.totalPrice?.currency ?? 'USD';
        const mapRoomTypeNumberToString = (n)=>{
          switch(n){
            case 0: return 'Standard';
            case 1: return 'Deluxe';
            case 2: return 'Suite';
            default: return 'Unknown';
          }
        };

        const payload = {
          guestName: name,
          destination: room.destinationCode ?? this.state.reservation.search.destination,
          documentType: docType,
          documentNumber: docNumber,
          provider: room.provider,
          roomType: (typeof room.roomType === 'number') ? mapRoomTypeNumberToString(room.roomType) : (room.roomType ?? 'Unknown'),
          checkIn: this.state.reservation.search.checkIn,
          checkOut: this.state.reservation.search.checkOut,
          ratePerNight: ratePerNight,
          currency: currency
        };

        // POST to the API reserve endpoint
        const res = await fetch(API_BASE + '/hotels/reserve', {method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)});
        if(res.status===422){ const txt = await res.text(); inlineErr.textContent = txt; return; }
        if(!res.ok){ const txt = await res.text(); throw new Error(txt || res.statusText); }
        const data = await res.json();
        // Normalize confirmation shape coming from POST (created) or GET (reservation confirmation)
        const confirm = {
          reservationId: data.reservationId ?? data.reservationId ?? data.reservationId,
          provider: data.provider ?? data.provider,
          guestName: data.guestName ?? data.guestName,
          // total may be returned as { amount, currency } under either 'total' or 'totalPrice'
          total: data.total ?? data.totalPrice ?? null,
          cancellationPolicy: data.cancellationPolicy ?? data.cancellationPolicy ?? null
        };
        this.state.reservation.confirm = confirm; this.setView('confirm');
      }catch(err){ inlineErr.textContent = err.message || 'Reservation failed'; }
    });
  },

  // ---------- Confirmation ----------
  renderConfirm(){
    const c = this.state.reservation.confirm;
    const card = this.el('div',{class:'card'});
    if(!c){ card.appendChild(this.el('div',{},'No confirmation data')); this.root.appendChild(card); return; }
    card.appendChild(this.el('h3',{},'Reservation confirmed'));
    card.appendChild(this.el('div',{}, this.el('strong',{},'Reference: '), c.reservationId || c.reservationId || ''));
    card.appendChild(this.el('div',{}, this.el('strong',{},'Provider: '), c.provider || this.state.reservation.room.provider));
    card.appendChild(this.el('div',{}, this.el('strong',{},'Guest: '), c.guestName || ''));
    const totalAmount = Number(c.total?.amount ?? c.total ?? 0) || 0;
    const totalCurrency = c.total?.currency ?? 'USD';
    card.appendChild(this.el('div',{}, this.el('strong',{},'Total: '), `${totalCurrency} ${totalAmount.toFixed(2)}`));
    const cancText = c.cancellationPolicy?.description ?? c.cancellationPolicy ?? this.state.reservation.room.cancellationPolicy ?? '';
    card.appendChild(this.el('div',{}, this.el('strong',{},'Cancellation: '), cancText));
    const viewAgain = this.el('a',{class:'link',onclick:async ()=>{
      // GET /hotels/reservation/{reference}
      const ref = c.reference || c.id; if(!ref) return; try{ const res = await fetch(API_BASE + '/hotels/reservation/' + encodeURIComponent(ref)); if(!res.ok) throw new Error(await res.text()||res.statusText); const data=await res.json(); this.state.reservation.confirm = data; this.setView('confirm'); }catch(err){ alert('Error: '+(err.message||'')); }
    }}, 'View again');
    card.appendChild(this.el('div',{}, viewAgain));
    card.appendChild(this.el('div',{}, this.el('button',{class:'btn',onclick:()=>this.setView('search')},'New search')));
    this.root.appendChild(card);
  }
};

document.addEventListener('DOMContentLoaded', ()=>App.init());
